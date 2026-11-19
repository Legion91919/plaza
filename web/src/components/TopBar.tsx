import type { Me } from '../api/types';
import type { LiveStatus } from '../live/useLiveNotifications';
import { Logo } from './Logo';

interface Props {
  users: Me[] | null;
  usersError: string | null;
  currentUserId: string;
  onSwitchUser: (id: string) => void;
  liveStatus: LiveStatus;
}

const liveLabel: Record<LiveStatus, string> = { connecting: 'Connecting…', live: 'Live', offline: 'Offline' };

export function TopBar({ users, usersError, currentUserId, onSwitchUser, liveStatus }: Props) {
  return (
    <header className="top">
      <div className="top-in">
        <Logo />
        <div className="who">
          <span className={`live live-${liveStatus}`} aria-live="polite">● {liveLabel[liveStatus]}</span>
          <label htmlFor="demo-user">Demo user</label>
          {usersError ? (
            <span className="muted">Can't reach Plaza</span>
          ) : (
            <select id="demo-user" value={currentUserId} disabled={!users} onChange={(e) => onSwitchUser(e.target.value)}>
              {!users && <option>Loading residents…</option>}
              {users?.map((u) => (
                <option key={u.userId} value={u.userId}>
                  {u.displayName}
                  {u.groups.some((g) => g.role === 'moderator') ? ' (moderator)' : ''}
                  {u.isVerified ? '' : ' (unverified)'}
                </option>
              ))}
            </select>
          )}
        </div>
      </div>
    </header>
  );
}
