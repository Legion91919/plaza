using Plaza.Feed.Contracts;

namespace Plaza.Feed.Infrastructure;

/// <summary>
/// Append-only by design: there is deliberately no update or delete. Entries are immutable records.
/// Slice 2 backs this with an insert-only table (no UPDATE/DELETE grants for the app user).
/// </summary>
internal interface IModerationLog
{
    ModerationEntry Append(string action, string groupId, string postId, string postAuthorId, string moderatorId, string reason, DateTimeOffset at);

    IReadOnlyList<ModerationEntry> ForGroup(string groupId);
}

internal sealed class InMemoryModerationLog : IModerationLog
{
    private readonly object _gate = new();
    private readonly List<ModerationEntry> _entries = [];

    public ModerationEntry Append(string action, string groupId, string postId, string postAuthorId, string moderatorId, string reason, DateTimeOffset at)
    {
        lock (_gate)
        {
            var entry = new ModerationEntry(_entries.Count + 1, action, groupId, postId, postAuthorId, moderatorId, reason, at);
            _entries.Add(entry);
            return entry;
        }
    }

    public IReadOnlyList<ModerationEntry> ForGroup(string groupId)
    {
        lock (_gate)
        {
            return _entries.Where(e => e.GroupId == groupId).ToList();
        }
    }
}
