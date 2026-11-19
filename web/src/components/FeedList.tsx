import type { Post } from '../api/types';
import { PostCard } from './PostCard';

export type Loadable<T> = { status: 'loading' } | { status: 'error'; message: string } | { status: 'success'; data: T };

interface Props { feed: Loadable<Post[]>; canModerate: boolean; onRemove: (p: Post) => void; onRetry: () => void }

export function FeedList({ feed, canModerate, onRemove, onRetry }: Props) {
  if (feed.status === 'loading') {
    return (
      <div className="feed" aria-busy="true" aria-label="Loading feed">
        {[0, 1, 2].map((i) => (
          <div key={i} className="card skel" aria-hidden="true"><div className="line w40" /><div className="line" /><div className="line w70" /></div>
        ))}
      </div>
    );
  }
  if (feed.status === 'error') {
    return (
      <div className="notice-box err" role="alert">
        <b>We couldn't load the feed.</b>
        <p>{feed.message}</p>
        <button className="btn" onClick={onRetry}>Try again</button>
      </div>
    );
  }
  if (feed.data.length === 0) {
    return <div className="notice-box"><b>No notices yet</b><p className="muted">Be the first to tell your neighbours something useful.</p></div>;
  }
  return (
    <section className="feed" aria-label="Group feed, newest first">
      {feed.data.map((p) => <PostCard key={p.postId} post={p} canModerate={canModerate} onRemove={onRemove} />)}
    </section>
  );
}
