using Microsoft.AspNetCore.Mvc;
using Plaza.Api.Auth;
using Plaza.Api.Http;
using Plaza.Events.Contracts;
using Plaza.Feed.Contracts;
using Plaza.Groups.Contracts;
using Plaza.Profiles.Contracts;
using Plaza.SharedKernel;

namespace Plaza.Api.Endpoints;

internal static class PlazaEndpoints
{
    public const string IdempotencyHeader = "Idempotency-Key";
    public const int DefaultFeedPage = 50;

    public static void MapPlazaApi(this IEndpointRouteBuilder app, bool demoAuth)
    {
        var api = app.MapGroup("/api").RequireAuthorization();

        api.MapGet("/me", (ICallerIdentity caller, IProfileDirectory profiles, IGroupDirectory groups) =>
        {
            var profile = profiles.Find(caller.UserId!);
            return profile is null
                ? OutcomeHttp.Problem(Failure.NotFound("profile_not_found", "No profile for this user."))
                : Results.Ok(ToMe(profile, groups));
        });

        var group = api.MapGroup("/groups/{groupId}");

        group.MapGet("/", (string groupId, ICallerIdentity caller, IGroupDirectory groups) =>
        {
            var summary = groups.FindGroup(groupId);
            if (summary is null)
            {
                return OutcomeHttp.Problem(Failure.NotFound("group_not_found", $"Group '{groupId}' does not exist."));
            }

            var role = groups.RoleOf(groupId, caller.UserId!);
            return role is null
                ? OutcomeHttp.Problem(Failure.Forbidden("not_a_member", "Only members can open this group."))
                : Results.Ok(new GroupDto(summary.GroupId, summary.Name, summary.Area, summary.MemberCount, role.Value));
        });

        group.MapGet("/posts", (string groupId, int? limit, ICallerIdentity caller, IFeedService feed) =>
            feed.GetFeed(groupId, caller.UserId!, limit ?? DefaultFeedPage).ToResult(Results.Ok));

        group.MapPost("/posts", async (
            string groupId,
            PostNoticeRequest request,
            [FromHeader(Name = IdempotencyHeader)] string? idempotencyKey,
            ICallerIdentity caller,
            IFeedService feed,
            HttpContext http,
            CancellationToken ct) =>
        {
            var outcome = await feed.PostNoticeAsync(
                groupId, caller.UserId!, new NewNotice(request.Body ?? string.Empty, request.Kind ?? PostKind.Notice, idempotencyKey), ct);
            return outcome.ToResult(created =>
            {
                if (created.Replayed)
                {
                    http.Response.Headers["Idempotent-Replayed"] = "true";
                    return Results.Ok(created.Post);
                }

                return Results.Created($"/api/groups/{groupId}/posts/{created.Post.PostId}", created.Post);
            });
        });

        group.MapPost("/posts/{postId}/removal", async (
            string groupId, string postId, RemovePostRequest request, ICallerIdentity caller, IFeedService feed, CancellationToken ct) =>
            (await feed.RemovePostAsync(groupId, postId, caller.UserId!, request.Reason ?? string.Empty, ct)).ToResult(Results.Ok));

        group.MapGet("/moderation-log", (string groupId, ICallerIdentity caller, IFeedService feed) =>
            feed.GetModerationLog(groupId, caller.UserId!).ToResult(Results.Ok));

        group.MapGet("/events", (string groupId, ICallerIdentity caller, IEventCalendar events) =>
            events.Upcoming(groupId, caller.UserId!).ToResult(Results.Ok));

        if (demoAuth)
        {
            // Development-only: lets the web demo switch between invented residents.
            app.MapGet("/api/demo/users", (IProfileDirectory profiles, IGroupDirectory groups) =>
                Results.Ok(profiles.List().Select(p => ToMe(p, groups)))).AllowAnonymous();
        }
    }

    private static MeDto ToMe(ResidentProfile profile, IGroupDirectory groups) =>
        new(
            profile.UserId,
            profile.DisplayName,
            profile.IsVerified,
            groups.MembershipsOf(profile.UserId)
                .Select(m => (Membership: m, Group: groups.FindGroup(m.GroupId)))
                .Where(x => x.Group is not null)
                .Select(x => new GroupMembershipDto(x.Group!.GroupId, x.Group.Name, x.Group.Area, x.Membership.Role))
                .ToList());
}
