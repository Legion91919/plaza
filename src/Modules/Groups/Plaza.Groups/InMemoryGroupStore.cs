using Plaza.Groups.Contracts;

namespace Plaza.Groups;

/// <summary>Thread-safe in-memory groups and memberships. Slice 2 moves this to Postgres.</summary>
internal sealed class InMemoryGroupStore : IGroupDirectory, IGroupAdministration
{
    private readonly object _gate = new();
    private readonly Dictionary<string, (string Name, string Area)> _groups = new(StringComparer.Ordinal);
    private readonly Dictionary<(string GroupId, string UserId), GroupRole> _members = [];

    public GroupSummary? FindGroup(string groupId)
    {
        lock (_gate)
        {
            if (!_groups.TryGetValue(groupId, out var g))
            {
                return null;
            }

            var count = _members.Keys.Count(k => k.GroupId == groupId);
            return new GroupSummary(groupId, g.Name, g.Area, count);
        }
    }

    public GroupRole? RoleOf(string groupId, string userId)
    {
        lock (_gate)
        {
            return _members.TryGetValue((groupId, userId), out var role) ? role : null;
        }
    }

    public IReadOnlyList<string> MemberIds(string groupId)
    {
        lock (_gate)
        {
            return _members.Keys.Where(k => k.GroupId == groupId).Select(k => k.UserId).ToList();
        }
    }

    public IReadOnlyList<Membership> MembershipsOf(string userId)
    {
        lock (_gate)
        {
            return _members
                .Where(m => m.Key.UserId == userId)
                .Select(m => new Membership(m.Key.GroupId, userId, m.Value))
                .OrderBy(m => m.GroupId, StringComparer.Ordinal)
                .ToList();
        }
    }

    public void CreateGroup(string groupId, string name, string area)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        lock (_gate)
        {
            _groups[groupId] = (name, area);
        }
    }

    public void AddMember(string groupId, string userId, GroupRole role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        lock (_gate)
        {
            if (!_groups.ContainsKey(groupId))
            {
                throw new InvalidOperationException($"Unknown group '{groupId}'.");
            }

            _members[(groupId, userId)] = role;
        }
    }
}
