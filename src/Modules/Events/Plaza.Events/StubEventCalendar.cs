using Plaza.Events.Contracts;
using Plaza.Groups.Contracts;
using Plaza.SharedKernel;

namespace Plaza.Events;

/// <summary>Stub: enforces membership like the real module will, but has no events yet.</summary>
internal sealed class StubEventCalendar(IGroupDirectory groups) : IEventCalendar
{
    public Outcome<IReadOnlyList<EventSummary>> Upcoming(string groupId, string viewerId)
    {
        if (groups.FindGroup(groupId) is null)
        {
            return Failure.NotFound("group_not_found", $"Group '{groupId}' does not exist.");
        }

        if (groups.RoleOf(groupId, viewerId) is null)
        {
            return Failure.Forbidden("not_a_member", "Only members can see this group's events.");
        }

        return Outcome.Ok<IReadOnlyList<EventSummary>>([]);
    }
}
