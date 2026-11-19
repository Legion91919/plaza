import type { EventSummary } from '../api/types';
import type { Loadable } from './FeedList';

export function EventsPanel({ events }: { events: Loadable<EventSummary[]> }) {
  return (
    <aside className="events" aria-label="Upcoming events">
      <h2>Upcoming events</h2>
      {events.status === 'loading' && <p className="muted small">Loading events…</p>}
      {events.status === 'error' && <p className="muted small">Events unavailable.</p>}
      {events.status === 'success' && events.data.length === 0 && <p className="muted small">No events yet. Events arrive in a later slice.</p>}
      {events.status === 'success' && events.data.map((e) => <p key={e.eventId}>{e.title}</p>)}
    </aside>
  );
}
