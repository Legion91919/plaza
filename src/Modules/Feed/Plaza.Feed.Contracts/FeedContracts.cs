using Plaza.SharedKernel;

namespace Plaza.Feed.Contracts;

public enum PostKind
{
    Notice = 0,
    SafetyAlert = 1,
}

/// <summary>A post as members see it in the group feed.</summary>
public sealed record PostView(
    string PostId,
    string GroupId,
    string AuthorId,
    string AuthorName,
    bool AuthorVerified,
    PostKind Kind,
    string Body,
    DateTimeOffset PostedAt);

/// <param name="IdempotencyKey">Client-chosen key; a retry with the same key returns the original post.</param>
public sealed record NewNotice(string Body, PostKind Kind, string? IdempotencyKey);

/// <param name="Replayed">True when the request was a retry of an earlier one with the same idempotency key.</param>
public sealed record PostCreated(PostView Post, bool Replayed);

/// <summary>One line of the append-only moderation log: who did what to which post, when and why.</summary>
public sealed record ModerationEntry(
    long Sequence,
    string Action,
    string GroupId,
    string PostId,
    string PostAuthorId,
    string ModeratorId,
    string Reason,
    DateTimeOffset At);

public static class ModerationActions
{
    public const string PostRemoved = "post.removed";
}

/// <summary>Use cases of the Feed module.</summary>
public interface IFeedService
{
    Task<Outcome<PostCreated>> PostNoticeAsync(string groupId, string authorId, NewNotice notice, CancellationToken cancellationToken);

    /// <summary>Visible posts, newest first. Members only.</summary>
    Outcome<IReadOnlyList<PostView>> GetFeed(string groupId, string viewerId, int limit);

    /// <summary>Moderators only. Hides the post and appends an entry to the moderation log.</summary>
    Task<Outcome<ModerationEntry>> RemovePostAsync(string groupId, string postId, string moderatorId, string reason, CancellationToken cancellationToken);

    /// <summary>Moderators only. Oldest first, exactly as appended.</summary>
    Outcome<IReadOnlyList<ModerationEntry>> GetModerationLog(string groupId, string viewerId);
}

/// <summary>Bulk import path (demo seeding now, data migration later). Skips live notifications.</summary>
public interface IFeedImport
{
    string Import(string groupId, string authorId, PostKind kind, string body, DateTimeOffset postedAt);
}
