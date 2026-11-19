using Microsoft.Extensions.Logging;
using Plaza.Feed.Contracts;
using Plaza.Feed.Domain;
using Plaza.Feed.Infrastructure;
using Plaza.Groups.Contracts;
using Plaza.Notifications.Contracts;
using Plaza.Profiles.Contracts;
using Plaza.SharedKernel;

namespace Plaza.Feed;

internal sealed partial class FeedService(
    IPostStore posts,
    IModerationLog moderationLog,
    IGroupDirectory groups,
    IProfileDirectory profiles,
    ILiveNotifier notifier,
    TimeProvider clock,
    ILogger<FeedService> logger) : IFeedService, IFeedImport
{
    public const int MaxIdempotencyKeyLength = 100;
    public const int MaxFeedPage = 100;

    public async Task<Outcome<PostCreated>> PostNoticeAsync(string groupId, string authorId, NewNotice notice, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notice);
        if (groups.FindGroup(groupId) is null)
        {
            return GroupNotFound(groupId);
        }

        var author = profiles.Find(authorId);
        if (FeedPolicy.CanPost(groups.RoleOf(groupId, authorId), author?.IsVerified ?? false) is { } denied)
        {
            return denied;
        }

        var key = string.IsNullOrWhiteSpace(notice.IdempotencyKey) ? null : notice.IdempotencyKey.Trim();
        if (key is { Length: > MaxIdempotencyKeyLength })
        {
            return Failure.Validation("idempotency_key_too_long", $"Idempotency-Key can be at most {MaxIdempotencyKeyLength} characters.");
        }

        var created = Post.Create(Guid.NewGuid().ToString("N"), groupId, authorId, notice.Kind, notice.Body, clock.GetUtcNow());
        if (!created.Succeeded)
        {
            return created.Failure!;
        }

        var (stored, replayed) = posts.AddOrGetExisting(created.Value, key);
        if (replayed && (stored.Body != created.Value.Body || stored.Kind != created.Value.Kind || stored.GroupId != groupId))
        {
            return Failure.Conflict("idempotency_key_reused", "This Idempotency-Key was already used for a different notice.");
        }

        var view = ToView(stored);
        if (!replayed)
        {
            await NotifySafelyAsync(
                new LiveNotification(
                    NotificationTypes.PostCreated, groupId, stored.Id, authorId, view.AuthorName, KindName(stored.Kind), stored.Preview(), stored.PostedAt),
                cancellationToken);
        }

        return Outcome.Ok(new PostCreated(view, replayed));
    }

    public Outcome<IReadOnlyList<PostView>> GetFeed(string groupId, string viewerId, int limit)
    {
        if (groups.FindGroup(groupId) is null)
        {
            return GroupNotFound(groupId);
        }

        if (FeedPolicy.CanRead(groups.RoleOf(groupId, viewerId)) is { } denied)
        {
            return denied;
        }

        if (limit is < 1 or > MaxFeedPage)
        {
            return Failure.Validation("limit_out_of_range", $"limit must be between 1 and {MaxFeedPage}.");
        }

        return Outcome.Ok<IReadOnlyList<PostView>>(posts.Visible(groupId, limit).Select(ToView).ToList());
    }

    public async Task<Outcome<ModerationEntry>> RemovePostAsync(string groupId, string postId, string moderatorId, string reason, CancellationToken cancellationToken)
    {
        if (groups.FindGroup(groupId) is null)
        {
            return GroupNotFound(groupId);
        }

        if (FeedPolicy.CanModerate(groups.RoleOf(groupId, moderatorId)) is { } denied)
        {
            return denied;
        }

        if (FeedPolicy.ValidateReason(reason) is { } invalid)
        {
            return invalid;
        }

        var now = clock.GetUtcNow();
        ModerationEntry? entry = null;
        var removed = posts.Change(groupId, postId, post =>
        {
            var result = post.Remove(moderatorId, now);
            if (result.Succeeded)
            {
                // Logged inside the same lock so the log and the post state can never disagree.
                entry = moderationLog.Append(ModerationActions.PostRemoved, groupId, postId, post.AuthorId, moderatorId, reason.Trim(), now);
            }

            return result;
        });

        if (!removed.Succeeded)
        {
            return removed.Failure!;
        }

        var moderatorName = profiles.Find(moderatorId)?.DisplayName ?? moderatorId;
        await NotifySafelyAsync(
            new LiveNotification(NotificationTypes.PostRemoved, groupId, postId, moderatorId, moderatorName, KindName(removed.Value.Kind), string.Empty, now),
            cancellationToken);

        return Outcome.Ok(entry!);
    }

    public Outcome<IReadOnlyList<ModerationEntry>> GetModerationLog(string groupId, string viewerId)
    {
        if (groups.FindGroup(groupId) is null)
        {
            return GroupNotFound(groupId);
        }

        if (FeedPolicy.CanModerate(groups.RoleOf(groupId, viewerId)) is { } denied)
        {
            return denied;
        }

        return Outcome.Ok(moderationLog.ForGroup(groupId));
    }

    public string Import(string groupId, string authorId, PostKind kind, string body, DateTimeOffset postedAt)
    {
        var created = Post.Create(Guid.NewGuid().ToString("N"), groupId, authorId, kind, body, postedAt);
        if (!created.Succeeded)
        {
            throw new ArgumentException(created.Failure!.Message, nameof(body));
        }

        return posts.AddOrGetExisting(created.Value, null).Stored.Id;
    }

    internal static string KindName(PostKind kind) => kind == PostKind.SafetyAlert ? "safetyAlert" : "notice";

    private static Failure GroupNotFound(string groupId) =>
        Failure.NotFound("group_not_found", $"Group '{groupId}' does not exist.");

    private PostView ToView(Post post)
    {
        var author = profiles.Find(post.AuthorId);
        return new PostView(
            post.Id, post.GroupId, post.AuthorId, author?.DisplayName ?? "Former resident", author?.IsVerified ?? false, post.Kind, post.Body, post.PostedAt);
    }

    /// <summary>
    /// A failed push must never fail the post itself: the notice is stored and members see it on refresh.
    /// Slice 2 moves this behind a transactional outbox so pushes are retried.
    /// </summary>
    private async Task NotifySafelyAsync(LiveNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            await notifier.NotifyGroupAsync(notification, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogNotifyFailed(logger, ex, notification.Type, notification.GroupId);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Live notification {Type} for group {GroupId} failed")]
    private static partial void LogNotifyFailed(ILogger logger, Exception ex, string type, string groupId);
}
