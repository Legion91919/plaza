namespace Plaza.Groups.Contracts;

/// <summary>A resident's role in one neighbourhood group.</summary>
public enum GroupRole
{
    Member = 1,
    Moderator = 2,
}

/// <summary>A neighbourhood group (one per neighbourhood association area).</summary>
public sealed record GroupSummary(string GroupId, string Name, string Area, int MemberCount);

public sealed record Membership(string GroupId, string UserId, GroupRole Role);

/// <summary>Read side of the Groups module, used by Feed, Events and Notifications.</summary>
public interface IGroupDirectory
{
    GroupSummary? FindGroup(string groupId);

    /// <summary>The user's role in the group, or null when they are not a member.</summary>
    GroupRole? RoleOf(string groupId, string userId);

    IReadOnlyList<string> MemberIds(string groupId);

    IReadOnlyList<Membership> MembershipsOf(string userId);
}

/// <summary>Write side used by association admins and the demo seeder.</summary>
public interface IGroupAdministration
{
    void CreateGroup(string groupId, string name, string area);

    void AddMember(string groupId, string userId, GroupRole role);
}
