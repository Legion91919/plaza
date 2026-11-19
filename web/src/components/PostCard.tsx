import type { Post } from '../api/types';
import { initials, relativeTime } from '../lib/time';

interface Props { post: Post; canModerate: boolean; onRemove: (post: Post) => void; now?: Date }

export function PostCard({ post, canModerate, onRemove, now }: Props) {
  const alert = post.kind === 'safetyAlert';
  return (
    <article className={`card${alert ? ' alertcard' : ''}`}>
      <header>
        <span className="av" aria-hidden="true">{initials(post.authorName)}</span>
        <div>
          <b>{post.authorName}</b>
          <div className="muted small">
            {post.authorVerified ? 'Verified resident' : 'Resident'} · <time dateTime={post.postedAt}>{relativeTime(post.postedAt, now)}</time>
          </div>
        </div>
        <span className={`tag${alert ? ' alert' : ''}`}>{alert ? 'Safety alert' : 'Notice'}</span>
      </header>
      <p>{post.body}</p>
      {canModerate && (
        <footer><button className="link" onClick={() => onRemove(post)}>Remove…</button></footer>
      )}
    </article>
  );
}
