using Plaza.Feed.Contracts;
using Plaza.SharedKernel;

namespace Plaza.Feed.Domain;

/// <summary>A notice in a neighbourhood group. Removal hides it; nothing is ever hard-deleted in this slice.</summary>
internal sealed class Post
{
    public const int MaxBodyLength = 1000;
    public const int MinBodyLength = 3;

    private Post(string id, string groupId, string authorId, PostKind kind, string body, DateTimeOffset postedAt)
    {
        Id = id;
        GroupId = groupId;
        AuthorId = authorId;
        Kind = kind;
        Body = body;
        PostedAt = postedAt;
    }

    public string Id { get; }

    public string GroupId { get; }

    public string AuthorId { get; }

    public PostKind Kind { get; }

    public string Body { get; }

    public DateTimeOffset PostedAt { get; }

    /// <summary>Insertion order; breaks ties when two posts share a timestamp.</summary>
    public long Sequence { get; set; }

    public DateTimeOffset? RemovedAt { get; private set; }

    public string? RemovedBy { get; private set; }

    public bool IsRemoved => RemovedAt is not null;

    public static Outcome<Post> Create(string id, string groupId, string authorId, PostKind kind, string? body, DateTimeOffset postedAt)
    {
        var text = body?.Trim() ?? string.Empty;
        if (text.Length < MinBodyLength)
        {
            return Failure.Validation("body_too_short", $"A notice needs at least {MinBodyLength} characters.");
        }

        if (text.Length > MaxBodyLength)
        {
            return Failure.Validation("body_too_long", $"A notice can be at most {MaxBodyLength} characters.");
        }

        if (!Enum.IsDefined(kind))
        {
            return Failure.Validation("unknown_kind", "Unknown post kind.");
        }

        return Outcome.Ok(new Post(id, groupId, authorId, kind, text, postedAt));
    }

    public Outcome<Post> Remove(string moderatorId, DateTimeOffset at)
    {
        if (IsRemoved)
        {
            return Failure.Conflict("already_removed", "This post was already removed.");
        }

        RemovedAt = at;
        RemovedBy = moderatorId;
        return Outcome.Ok(this);
    }

    /// <summary>Short text for notifications: one line, at most 140 characters.</summary>
    public string Preview()
    {
        var oneLine = Body.ReplaceLineEndings(" ");
        return oneLine.Length <= 140 ? oneLine : string.Concat(oneLine.AsSpan(0, 139), "…");
    }
}
