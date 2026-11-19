using Plaza.SharedKernel;

namespace Plaza.Events.Contracts;

public sealed record EventSummary(string EventId, string GroupId, string Title, DateTimeOffset StartsAt, string Venue);

/// <summary>Neighbourhood events. Slice 1 ships the boundary only; the real calendar arrives in a later slice.</summary>
public interface IEventCalendar
{
    Outcome<IReadOnlyList<EventSummary>> Upcoming(string groupId, string viewerId);
}
