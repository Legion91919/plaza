using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Plaza.Api.Seeding;
using Plaza.Notifications.Contracts;
using Plaza.Tests.Support;
using Xunit;

namespace Plaza.Acceptance.Tests;

/// <summary>Slice 1 acceptance criteria, end to end over real HTTP and WebSockets.</summary>
public class Slice1AcceptanceTests
{
    private const string Feed = "/api/groups/lindehof/posts";

    // AC1: Given a resident is a member of a group, when they post, then the post appears in the group feed newest-first.
    [Fact]
    public async Task Ac1_member_post_appears_at_the_top_of_the_feed()
    {
        await using var app = await TestApp.StartAsync();
        var pieter = app.ClientFor(DemoSeeder.Pieter);

        var post = await pieter.PostAsJsonAsync(Feed, new { body = "Found: a set of keys on Acacia Lane.", kind = "notice" });
        var feed = await TestApp.ReadJsonAsync(await app.ClientFor(DemoSeeder.Zanele).GetAsync(Feed));

        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        var created = await TestApp.ReadJsonAsync(post);
        Assert.StartsWith("/api/groups/lindehof/posts/", post.Headers.Location!.OriginalString);
        Assert.Equal(created.GetProperty("postId").GetString(), feed[0].GetProperty("postId").GetString());
        Assert.Equal("Pieter Okafor", feed[0].GetProperty("authorName").GetString());
        var times = feed.EnumerateArray().Select(p => p.GetProperty("postedAt").GetDateTimeOffset()).ToList();
        Assert.Equal(times.OrderByDescending(t => t).ToList(), times);
        Assert.Equal(5, times.Count);
    }

    [Fact]
    public async Task Ac1_retry_with_the_same_idempotency_key_does_not_duplicate()
    {
        await using var app = await TestApp.StartAsync();
        var pieter = app.ClientFor(DemoSeeder.Pieter);

        async Task<HttpResponseMessage> Send()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Feed)
            {
                Content = JsonContent.Create(new { body = "Spare boxes at number 12, free.", kind = "notice" }),
            };
            request.Headers.Add("Idempotency-Key", "retry-1");
            return await pieter.SendAsync(request);
        }

        var first = await Send();
        var second = await Send();

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(
            (await TestApp.ReadJsonAsync(first)).GetProperty("postId").GetString(),
            (await TestApp.ReadJsonAsync(second)).GetProperty("postId").GetString());
        Assert.Equal(5, (await TestApp.ReadJsonAsync(await pieter.GetAsync(Feed))).GetArrayLength());
    }

    [Fact]
    public async Task Ac1_unverified_member_can_read_but_not_post()
    {
        await using var app = await TestApp.StartAsync();
        var jonas = app.ClientFor(DemoSeeder.Jonas);

        var read = await jonas.GetAsync(Feed);
        var post = await jonas.PostAsJsonAsync(Feed, new { body = "Hello, I am new here." });

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, post.StatusCode);
        Assert.Equal("not_verified", (await TestApp.ReadJsonAsync(post)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Ac1_invalid_notice_is_a_problem_details_400()
    {
        await using var app = await TestApp.StartAsync();

        var post = await app.ClientFor(DemoSeeder.Pieter).PostAsJsonAsync(Feed, new { body = "x" });

        Assert.Equal(HttpStatusCode.BadRequest, post.StatusCode);
        Assert.Equal("application/problem+json", post.Content.Headers.ContentType!.MediaType);
        Assert.Equal("body_too_short", (await TestApp.ReadJsonAsync(post)).GetProperty("code").GetString());
    }

    // AC2: Given a non-member, then they can't read the group feed (403).
    [Fact]
    public async Task Ac2_non_member_gets_403_on_the_feed()
    {
        await using var app = await TestApp.StartAsync();
        var ayesha = app.ClientFor(DemoSeeder.Ayesha);

        var read = await ayesha.GetAsync(Feed);
        var post = await ayesha.PostAsJsonAsync(Feed, new { body = "Not my neighbourhood." });
        var group = await ayesha.GetAsync("/api/groups/lindehof");

        Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);
        Assert.Equal("application/problem+json", read.Content.Headers.ContentType!.MediaType);
        Assert.Equal("not_a_member", (await TestApp.ReadJsonAsync(read)).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, post.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, group.StatusCode);
    }

    [Fact]
    public async Task Ac2_anonymous_and_unknown_callers_get_401()
    {
        await using var app = await TestApp.StartAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await app.ClientFor(null).GetAsync(Feed)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await app.ClientFor("u-nobody").GetAsync(Feed)).StatusCode);
    }

    // AC3: Given a new post, then online members receive a live notification.
    [Fact]
    public async Task Ac3_online_members_receive_a_live_notification_and_outsiders_do_not()
    {
        await using var app = await TestApp.StartAsync();
        await using var zanele = await RawSignalRClient.ConnectAsync(app.BaseAddress, NotificationChannels.HubPath, DemoSeeder.Zanele);
        await using var ayesha = await RawSignalRClient.ConnectAsync(app.BaseAddress, NotificationChannels.HubPath, DemoSeeder.Ayesha);
        var zaneleGroups = await zanele.InvokeAsync("Subscriptions");
        var ayeshaGroups = await ayesha.InvokeAsync("Subscriptions");

        await app.ClientFor(DemoSeeder.Pieter).PostAsJsonAsync(Feed, new { body = "Burst pipe on Vink Street, avoid the area.", kind = "safetyAlert" });

        var received = await zanele.WaitForInvocationAsync(NotificationChannels.ClientMethod, TimeSpan.FromSeconds(5));
        Assert.NotNull(received);
        Assert.Equal(NotificationTypes.PostCreated, received!.Value.GetProperty("type").GetString());
        Assert.Equal("lindehof", received.Value.GetProperty("groupId").GetString());
        Assert.Equal("safetyAlert", received.Value.GetProperty("kind").GetString());
        Assert.Equal("Pieter Okafor", received.Value.GetProperty("authorName").GetString());
        Assert.Contains("lindehof", zaneleGroups.EnumerateArray().Select(g => g.GetString()));
        Assert.DoesNotContain(ayeshaGroups.EnumerateArray().Select(g => g.GetString()), g => g == "lindehof");
        Assert.Null(await ayesha.WaitForInvocationAsync(NotificationChannels.ClientMethod, TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task Ac3_hub_rejects_anonymous_connections()
    {
        await using var app = await TestApp.StartAsync();

        var negotiate = await app.ClientFor(null).PostAsync(new Uri($"{NotificationChannels.HubPath}/negotiate?negotiateVersion=1", UriKind.Relative), null);

        Assert.Equal(HttpStatusCode.Unauthorized, negotiate.StatusCode);
    }

    // AC4: Given a moderator removes a post, then it disappears from the feed and the removal is logged with who and why.
    [Fact]
    public async Task Ac4_moderator_removal_hides_the_post_and_is_logged_with_who_when_and_why()
    {
        await using var app = await TestApp.StartAsync();
        var naledi = app.ClientFor(DemoSeeder.Naledi);
        var target = (await TestApp.ReadJsonAsync(await naledi.GetAsync(Feed)))[0].GetProperty("postId").GetString();
        app.Clock.Advance(TimeSpan.FromMinutes(3));

        var removal = await naledi.PostAsJsonAsync($"{Feed}/{target}/removal", new { reason = "Duplicate of the earlier clean-up notice" });
        var feed = await TestApp.ReadJsonAsync(await app.ClientFor(DemoSeeder.Pieter).GetAsync(Feed));
        var log = await TestApp.ReadJsonAsync(await naledi.GetAsync("/api/groups/lindehof/moderation-log"));

        Assert.Equal(HttpStatusCode.OK, removal.StatusCode);
        Assert.DoesNotContain(feed.EnumerateArray().Select(p => p.GetProperty("postId").GetString()), id => id == target);
        var entry = Assert.Single(log.EnumerateArray());
        Assert.Equal(target, entry.GetProperty("postId").GetString());
        Assert.Equal(DemoSeeder.Naledi, entry.GetProperty("moderatorId").GetString());
        Assert.Equal("Duplicate of the earlier clean-up notice", entry.GetProperty("reason").GetString());
        Assert.Equal(ManualTimeProvider.DefaultStart.AddMinutes(3), entry.GetProperty("at").GetDateTimeOffset());
    }

    [Fact]
    public async Task Ac4_members_cannot_remove_posts_or_read_the_moderation_log()
    {
        await using var app = await TestApp.StartAsync();
        var pieter = app.ClientFor(DemoSeeder.Pieter);
        var target = (await TestApp.ReadJsonAsync(await pieter.GetAsync(Feed)))[0].GetProperty("postId").GetString();

        var removal = await pieter.PostAsJsonAsync($"{Feed}/{target}/removal", new { reason = "I simply disagree" });
        var log = await pieter.GetAsync("/api/groups/lindehof/moderation-log");

        Assert.Equal(HttpStatusCode.Forbidden, removal.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, log.StatusCode);
        Assert.Equal(4, (await TestApp.ReadJsonAsync(await pieter.GetAsync(Feed))).GetArrayLength());
    }

    [Fact]
    public async Task Ac4_removal_without_a_reason_is_rejected()
    {
        await using var app = await TestApp.StartAsync();
        var naledi = app.ClientFor(DemoSeeder.Naledi);
        var target = (await TestApp.ReadJsonAsync(await naledi.GetAsync(Feed)))[0].GetProperty("postId").GetString();

        var removal = await naledi.PostAsJsonAsync($"{Feed}/{target}/removal", new { reason = "" });

        Assert.Equal(HttpStatusCode.BadRequest, removal.StatusCode);
        Assert.Equal("reason_required", (await TestApp.ReadJsonAsync(removal)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Me_lists_memberships_and_health_endpoints_answer()
    {
        await using var app = await TestApp.StartAsync();
        var anon = app.ClientFor(null);

        var me = await TestApp.ReadJsonAsync(await app.ClientFor(DemoSeeder.Zanele).GetAsync("/api/me"));

        Assert.Equal(HttpStatusCode.OK, (await anon.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anon.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(2, me.GetProperty("groups").GetArrayLength());
        Assert.Equal(JsonValueKind.Array, (await TestApp.ReadJsonAsync(await anon.GetAsync("/api/demo/users"))).ValueKind);
        Assert.Equal(HttpStatusCode.OK, (await app.ClientFor(DemoSeeder.Zanele).GetAsync("/api/groups/lindehof/events")).StatusCode);
    }
}
