using Microsoft.Extensions.Logging.Abstractions;
using Plaza.Feed;
using Plaza.Feed.Contracts;
using Plaza.Feed.Infrastructure;
using Plaza.Groups.Contracts;
using Plaza.Notifications.Contracts;
using Plaza.Profiles.Contracts;
using Plaza.SharedKernel;
using Plaza.Tests.Support;
using Xunit;

namespace Plaza.Application.Tests;

public class FeedServiceTests
{
    private const string Group = "g-test";
    private const string Moderator = "mod";
    private const string Member = "member";
    private const string Unverified = "unverified";
    private const string Outsider = "outsider";

    private readonly ManualTimeProvider _clock = new(ManualTimeProvider.DefaultStart);
    private readonly RecordingNotifier _notifier = new();
    private readonly InMemoryModerationLog _log = new();

    [Fact]
    public async Task Posts_come_back_newest_first_even_with_equal_timestamps()
    {
        var feed = CreateService();
        await PostAsync(feed, "First notice");
        _clock.Advance(TimeSpan.FromMinutes(1));
        await PostAsync(feed, "Second notice");
        await PostAsync(feed, "Third notice, same minute");

        var bodies = feed.GetFeed(Group, Member, 50).Value.Select(p => p.Body).ToList();

        Assert.Equal(["Third notice, same minute", "Second notice", "First notice"], bodies);
    }

    [Fact]
    public async Task Unverified_member_cannot_post()
    {
        var result = await CreateService().PostNoticeAsync(Group, Unverified, new NewNotice("Hello neighbours", PostKind.Notice, null), default);

        Assert.Equal("not_verified", result.Failure!.Code);
        Assert.Empty(_notifier.Sent);
    }

    [Fact]
    public void Feed_limit_is_validated()
    {
        var feed = CreateService();

        Assert.Equal("limit_out_of_range", feed.GetFeed(Group, Member, 0).Failure!.Code);
        Assert.Equal("limit_out_of_range", feed.GetFeed(Group, Member, 101).Failure!.Code);
    }

    [Fact]
    public async Task Retrying_with_the_same_idempotency_key_returns_the_original_post_and_notifies_once()
    {
        var feed = CreateService();
        var notice = new NewNotice("Gate code changes on Monday", PostKind.Notice, "key-1");

        var first = await feed.PostNoticeAsync(Group, Member, notice, default);
        var retry = await feed.PostNoticeAsync(Group, Member, notice, default);

        Assert.False(first.Value.Replayed);
        Assert.True(retry.Value.Replayed);
        Assert.Equal(first.Value.Post.PostId, retry.Value.Post.PostId);
        Assert.Single(feed.GetFeed(Group, Member, 50).Value);
        Assert.Single(_notifier.Sent);
    }

    [Fact]
    public async Task Reusing_an_idempotency_key_for_different_content_is_a_conflict()
    {
        var feed = CreateService();
        await feed.PostNoticeAsync(Group, Member, new NewNotice("Original text", PostKind.Notice, "key-1"), default);

        var reuse = await feed.PostNoticeAsync(Group, Member, new NewNotice("Different text", PostKind.Notice, "key-1"), default);

        Assert.Equal(FailureKind.Conflict, reuse.Failure!.Kind);
    }

    [Fact]
    public async Task A_new_post_sends_a_live_notification_to_the_group()
    {
        var feed = CreateService();

        var created = await feed.PostNoticeAsync(Group, Member, new NewNotice("Load shedding tonight 18:00", PostKind.SafetyAlert, null), default);

        var sent = Assert.Single(_notifier.Sent);
        Assert.Equal(NotificationTypes.PostCreated, sent.Type);
        Assert.Equal(Group, sent.GroupId);
        Assert.Equal(created.Value.Post.PostId, sent.PostId);
        Assert.Equal("Member Name", sent.AuthorName);
        Assert.Equal("safetyAlert", sent.Kind);
    }

    [Fact]
    public async Task A_failing_push_does_not_fail_the_post()
    {
        _notifier.FailWith = new InvalidOperationException("hub down");
        var feed = CreateService();

        var created = await feed.PostNoticeAsync(Group, Member, new NewNotice("Still stored", PostKind.Notice, null), default);

        Assert.True(created.Succeeded);
        Assert.Single(feed.GetFeed(Group, Member, 50).Value);
    }

    [Fact]
    public async Task Moderator_removal_hides_the_post_and_logs_who_when_and_why()
    {
        var feed = CreateService();
        var post = (await PostAsync(feed, "Selling sofas, DM me")).Post;
        _clock.Advance(TimeSpan.FromMinutes(10));

        var entry = (await feed.RemovePostAsync(Group, post.PostId, Moderator, "  Advertising is not allowed  ", default)).Value;

        Assert.Empty(feed.GetFeed(Group, Member, 50).Value);
        Assert.Equal(Moderator, entry.ModeratorId);
        Assert.Equal(Member, entry.PostAuthorId);
        Assert.Equal("Advertising is not allowed", entry.Reason);
        Assert.Equal(ManualTimeProvider.DefaultStart.AddMinutes(10), entry.At);
        Assert.Equal(ModerationActions.PostRemoved, entry.Action);
        Assert.Equal(entry, Assert.Single(feed.GetModerationLog(Group, Moderator).Value));
        Assert.Contains(_notifier.Sent, n => n.Type == NotificationTypes.PostRemoved && n.Preview.Length == 0);
    }

    [Fact]
    public async Task Members_cannot_remove_posts_or_read_the_log()
    {
        var feed = CreateService();
        var post = (await PostAsync(feed, "A perfectly fine notice")).Post;

        var removal = await feed.RemovePostAsync(Group, post.PostId, Member, "I do not like it", default);

        Assert.Equal("not_a_moderator", removal.Failure!.Code);
        Assert.Equal("not_a_moderator", feed.GetModerationLog(Group, Member).Failure!.Code);
        Assert.Single(feed.GetFeed(Group, Member, 50).Value);
    }

    [Fact]
    public async Task Removing_twice_is_a_conflict_and_logs_only_once()
    {
        var feed = CreateService();
        var post = (await PostAsync(feed, "Duplicate notice")).Post;
        await feed.RemovePostAsync(Group, post.PostId, Moderator, "Duplicate of an earlier notice", default);

        var again = await feed.RemovePostAsync(Group, post.PostId, Moderator, "Duplicate of an earlier notice", default);

        Assert.Equal(FailureKind.Conflict, again.Failure!.Kind);
        Assert.Single(_log.ForGroup(Group));
    }

    [Fact]
    public void Moderation_log_is_append_only()
    {
        var methods = typeof(IModerationLog).GetMethods().Select(m => m.Name).ToList();

        Assert.Equal(["Append", "ForGroup"], methods.Order().ToList());
        foreach (var property in typeof(ModerationEntry).GetProperties())
        {
            var initOnly = property.SetMethod is null ||
                property.SetMethod.ReturnParameter.GetRequiredCustomModifiers().Any(m => m.Name == "IsExternalInit");
            Assert.True(initOnly, $"{property.Name} must not be settable after creation");
        }
    }

    [Fact]
    public void Moderation_log_keeps_append_order_and_sequence()
    {
        var at = ManualTimeProvider.DefaultStart;
        _log.Append(ModerationActions.PostRemoved, Group, "p1", "a", Moderator, "first reason", at);
        _log.Append(ModerationActions.PostRemoved, "other", "p2", "a", Moderator, "other group", at);
        _log.Append(ModerationActions.PostRemoved, Group, "p3", "a", Moderator, "third reason", at);

        var entries = _log.ForGroup(Group);

        Assert.Equal(["p1", "p3"], entries.Select(e => e.PostId).ToList());
        Assert.Equal([1L, 3L], entries.Select(e => e.Sequence).ToList());
    }

    private static Task<PostCreated> PostAsync(FeedService feed, string body) =>
        feed.PostNoticeAsync(Group, Member, new NewNotice(body, PostKind.Notice, null), default)
            .ContinueWith(t => t.Result.Value, TaskScheduler.Default);

    private FeedService CreateService()
    {
        var groups = new FakeGroups();
        var profiles = new FakeProfiles();
        return new FeedService(new InMemoryPostStore(), _log, groups, profiles, _notifier, _clock, NullLogger<FeedService>.Instance);
    }

    private sealed class RecordingNotifier : ILiveNotifier
    {
        public List<LiveNotification> Sent { get; } = [];

        public Exception? FailWith { get; set; }

        public Task NotifyGroupAsync(LiveNotification notification, CancellationToken cancellationToken)
        {
            if (FailWith is not null)
            {
                throw FailWith;
            }

            Sent.Add(notification);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGroups : IGroupDirectory
    {
        public GroupSummary? FindGroup(string groupId) => groupId == Group ? new GroupSummary(Group, "Test group", "Test area", 3) : null;

        public GroupRole? RoleOf(string groupId, string userId) => (groupId, userId) switch
        {
            (Group, Moderator) => GroupRole.Moderator,
            (Group, Member or Unverified) => GroupRole.Member,
            _ => null,
        };

        public IReadOnlyList<string> MemberIds(string groupId) => [Moderator, Member, Unverified];

        public IReadOnlyList<Membership> MembershipsOf(string userId) => [];
    }

    private sealed class FakeProfiles : IProfileDirectory
    {
        public ResidentProfile? Find(string userId) => userId switch
        {
            Moderator => new ResidentProfile(Moderator, "Moderator Name", true),
            Member => new ResidentProfile(Member, "Member Name", true),
            Unverified => new ResidentProfile(Unverified, "Unverified Name", false),
            Outsider => new ResidentProfile(Outsider, "Outsider Name", true),
            _ => null,
        };

        public IReadOnlyList<ResidentProfile> List() => [];
    }
}
