import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { ApiError, createApiClient } from './api/client';
import type { EventSummary, Group, LiveNotification, Me, Post, PostKind } from './api/types';
import { addPost, applyNotification, newestFirst, shouldToast } from './lib/feed';
import { useLiveNotifications } from './live/useLiveNotifications';
import { TopBar } from './components/TopBar';
import { Composer } from './components/Composer';
import { FeedList, type Loadable } from './components/FeedList';
import { RemoveDialog } from './components/RemoveDialog';
import { Toasts, type ToastItem } from './components/Toasts';
import { EventsPanel } from './components/EventsPanel';

const DEFAULT_USER = 'u-naledi';

function messageOf(err: unknown): string {
  if (err instanceof ApiError && err.code === 'not_a_member') return "You're not a member of this group, so its feed is private.";
  return err instanceof Error ? err.message : 'Something went wrong.';
}

export function App() {
  const [users, setUsers] = useState<Me[] | null>(null);
  const [usersError, setUsersError] = useState<string | null>(null);
  const [userId, setUserId] = useState(() => localStorage.getItem('plaza.demoUser') ?? DEFAULT_USER);
  const [groupId, setGroupId] = useState<string | null>(null);
  const [group, setGroup] = useState<Loadable<Group>>({ status: 'loading' });
  const [feed, setFeed] = useState<Loadable<Post[]>>({ status: 'loading' });
  const [events, setEvents] = useState<Loadable<EventSummary[]>>({ status: 'loading' });
  const [removing, setRemoving] = useState<Post | null>(null);
  const [toasts, setToasts] = useState<ToastItem[]>([]);

  const api = useMemo(() => createApiClient(userId), [userId]);
  const me = users?.find((u) => u.userId === userId) ?? null;

  // Every group any demo user belongs to, so you can also see what a non-member gets (403).
  const allGroups = useMemo(() => {
    const map = new Map<string, string>();
    users?.forEach((u) => u.groups.forEach((g) => map.set(g.groupId, g.name)));
    return [...map].map(([id, name]) => ({ id, name }));
  }, [users]);

  useEffect(() => {
    api.demoUsers().then(setUsers).catch((e) => setUsersError(messageOf(e)));
  }, [api]);

  useEffect(() => {
    localStorage.setItem('plaza.demoUser', userId);
    if (me && !groupId) setGroupId(me.groups[0]?.groupId ?? allGroups[0]?.id ?? null);
  }, [userId, me, groupId, allGroups]);

  const loadFeed = useCallback(() => {
    if (!groupId) return;
    setFeed((f) => (f.status === 'success' ? f : { status: 'loading' }));
    api.feed(groupId).then((p) => setFeed({ status: 'success', data: newestFirst(p) })).catch((e) => setFeed({ status: 'error', message: messageOf(e) }));
  }, [api, groupId]);

  useEffect(() => {
    if (!groupId) return;
    setGroup({ status: 'loading' });
    setFeed({ status: 'loading' });
    setEvents({ status: 'loading' });
    api.group(groupId).then((g) => setGroup({ status: 'success', data: g })).catch((e) => setGroup({ status: 'error', message: messageOf(e) }));
    api.events(groupId).then((ev) => setEvents({ status: 'success', data: ev })).catch((e) => setEvents({ status: 'error', message: messageOf(e) }));
    loadFeed();
  }, [api, groupId, loadFeed]);

  // Live notifications: the handler reads current state through refs so the hub connection isn't rebuilt.
  const state = useRef({ groupId, userId, allGroups, loadFeed });
  state.current = { groupId, userId, allGroups, loadFeed };
  const onNotification = useCallback((n: LiveNotification) => {
    const s = state.current;
    if (n.groupId === s.groupId) {
      setFeed((f) => {
        if (f.status !== 'success') return f;
        const { posts, refetch } = applyNotification(f.data, n);
        if (refetch) s.loadFeed();
        return { status: 'success', data: posts };
      });
    }
    if (shouldToast(n, s.userId)) {
      const groupName = s.allGroups.find((g) => g.id === n.groupId)?.name ?? n.groupId;
      setToasts((t) => [...t.slice(-2), { id: `${n.postId}-${n.type}`, groupName, n }]);
    }
  }, []);
  const liveStatus = useLiveNotifications(userId, onNotification);
  const dismiss = useCallback((id: string) => setToasts((t) => t.filter((x) => x.id !== id)), []);

  async function submit(body: string, kind: PostKind, key: string) {
    if (!groupId) return;
    const post = await api.post(groupId, body, kind, key);
    setFeed((f) => (f.status === 'success' ? { status: 'success', data: addPost(f.data, post) } : f));
  }

  async function confirmRemove(reason: string) {
    if (!groupId || !removing) return;
    await api.remove(groupId, removing.postId, reason);
    setFeed((f) => (f.status === 'success' ? { status: 'success', data: f.data.filter((p) => p.postId !== removing.postId) } : f));
    setRemoving(null);
  }

  const role = group.status === 'success' ? group.data.yourRole : null;
  const canPost = role !== null && !!me?.isVerified;

  return (
    <>
      <TopBar users={users} usersError={usersError} currentUserId={userId} liveStatus={liveStatus}
        onSwitchUser={(id) => { setUserId(id); setGroupId(null); setToasts([]); }} />
      <div className="wrap">
        {allGroups.length > 1 && (
          <nav className="tabs" aria-label="Neighbourhood groups">
            {allGroups.map((g) => (
              <button key={g.id} aria-current={g.id === groupId ? 'page' : undefined} onClick={() => setGroupId(g.id)}>{g.name}</button>
            ))}
          </nav>
        )}
        <main>
          {group.status === 'loading' && <div className="skel-title" aria-busy="true" />}
          {group.status === 'error' && <div className="notice-box err" role="alert"><b>{group.message}</b></div>}
          {group.status === 'success' && (
            <>
              <p className="muted crumb">{group.data.area} · {group.data.memberCount} members · you are a {group.data.yourRole}</p>
              <h1>{group.data.name}</h1>
              <p className="muted sub">Notices from verified neighbours, newest first.</p>
              <Composer key={`${userId}-${groupId}`} groupName={group.data.area} canPost={canPost} onSubmit={submit}
                reasonIfNot="Verify your address to post here. You can still read every notice." />
            </>
          )}
          {group.status !== 'error' && (
            <div className="layout">
              <FeedList feed={feed} canModerate={role === 'moderator'} onRemove={setRemoving} onRetry={loadFeed} />
              <EventsPanel events={events} />
            </div>
          )}
        </main>
      </div>
      {removing && <RemoveDialog post={removing} onConfirm={confirmRemove} onCancel={() => setRemoving(null)} />}
      <Toasts items={toasts} onDismiss={dismiss} />
    </>
  );
}
