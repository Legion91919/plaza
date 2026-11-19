using Plaza.Feed.Domain;
using Plaza.SharedKernel;

namespace Plaza.Feed.Infrastructure;

internal interface IPostStore
{
    /// <summary>Stores the post, or returns the post already stored under the same author + idempotency key.</summary>
    (Post Stored, bool AlreadyExisted) AddOrGetExisting(Post post, string? idempotencyKey);

    /// <summary>Visible (not removed) posts, newest first.</summary>
    IReadOnlyList<Post> Visible(string groupId, int limit);

    /// <summary>Applies a change to one post atomically.</summary>
    Outcome<Post> Change(string groupId, string postId, Func<Post, Outcome<Post>> change);
}

/// <summary>Thread-safe in-memory posts. Slice 2 swaps it for Postgres behind <see cref="IPostStore"/>.</summary>
internal sealed class InMemoryPostStore : IPostStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Post> _posts = new(StringComparer.Ordinal);
    private readonly Dictionary<(string AuthorId, string Key), string> _idempotency = [];
    private long _sequence;

    public (Post Stored, bool AlreadyExisted) AddOrGetExisting(Post post, string? idempotencyKey)
    {
        ArgumentNullException.ThrowIfNull(post);
        lock (_gate)
        {
            if (idempotencyKey is not null)
            {
                if (_idempotency.TryGetValue((post.AuthorId, idempotencyKey), out var existingId))
                {
                    return (_posts[existingId], true);
                }

                _idempotency[(post.AuthorId, idempotencyKey)] = post.Id;
            }

            post.Sequence = ++_sequence;
            _posts[post.Id] = post;
            return (post, false);
        }
    }

    public IReadOnlyList<Post> Visible(string groupId, int limit)
    {
        lock (_gate)
        {
            return _posts.Values
                .Where(p => p.GroupId == groupId && !p.IsRemoved)
                .OrderByDescending(p => p.PostedAt)
                .ThenByDescending(p => p.Sequence)
                .Take(limit)
                .ToList();
        }
    }

    public Outcome<Post> Change(string groupId, string postId, Func<Post, Outcome<Post>> change)
    {
        ArgumentNullException.ThrowIfNull(change);
        lock (_gate)
        {
            if (!_posts.TryGetValue(postId, out var post) || post.GroupId != groupId)
            {
                return Failure.NotFound("post_not_found", "That post does not exist in this group.");
            }

            return change(post);
        }
    }
}
