import type { LiveNotification, Post } from '../api/types';

/** Newest first; ties keep the order the server sent. */
export function newestFirst(posts: Post[]): Post[] {
  return posts
    .map((p, i) => ({ p, i }))
    .sort((a, b) => Date.parse(b.p.postedAt) - Date.parse(a.p.postedAt) || a.i - b.i)
    .map((x) => x.p);
}

/** Adds a post to the feed once (idempotent) and keeps newest-first order. */
export function addPost(posts: Post[], post: Post): Post[] {
  return posts.some((p) => p.postId === post.postId) ? posts : newestFirst([post, ...posts]);
}

/** Applies a live notification: removals drop the post; creations ask for a refetch (the push carries a preview only). */
export function applyNotification(posts: Post[], n: LiveNotification): { posts: Post[]; refetch: boolean } {
  if (n.type === 'post.removed') return { posts: posts.filter((p) => p.postId !== n.postId), refetch: false };
  return { posts, refetch: !posts.some((p) => p.postId === n.postId) };
}

/** Toasts are for other people's posts in the group you're looking at or any of your groups. */
export function shouldToast(n: LiveNotification, currentUserId: string): boolean {
  return n.type === 'post.created' && n.authorId !== currentUserId;
}

export const MIN_BODY = 3;
export const MAX_BODY = 1000;

export function validateBody(body: string): string | null {
  const len = body.trim().length;
  if (len < MIN_BODY) return `Write at least ${MIN_BODY} characters.`;
  if (len > MAX_BODY) return `Keep it under ${MAX_BODY} characters.`;
  return null;
}
