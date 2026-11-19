import { addPost, applyNotification, newestFirst, shouldToast, validateBody } from './feed';
import type { LiveNotification, Post } from '../api/types';

const post = (postId: string, postedAt: string, authorId = 'u-a'): Post => ({
  postId, groupId: 'lindehof', authorId, authorName: 'A', authorVerified: true, kind: 'notice', body: 'body', postedAt,
});
const note = (type: LiveNotification['type'], postId: string, authorId = 'u-b'): LiveNotification => ({
  type, groupId: 'lindehof', postId, authorId, authorName: 'B', kind: 'notice', preview: 'p', occurredAt: '2026-11-19T10:00:00Z',
});

describe('feed ordering', () => {
  it('sorts newest first and keeps server order for ties', () => {
    const sorted = newestFirst([post('old', '2026-11-18T10:00:00Z'), post('tie1', '2026-11-19T10:00:00Z'), post('tie2', '2026-11-19T10:00:00Z')]);
    expect(sorted.map((p) => p.postId)).toEqual(['tie1', 'tie2', 'old']);
  });

  it('adds my new post at the top exactly once', () => {
    const feed = [post('a', '2026-11-18T10:00:00Z')];
    const mine = post('b', '2026-11-19T10:00:00Z');
    const once = addPost(feed, mine);
    expect(once.map((p) => p.postId)).toEqual(['b', 'a']);
    expect(addPost(once, mine)).toBe(once);
  });
});

describe('live notifications', () => {
  it('drops removed posts without refetching', () => {
    const result = applyNotification([post('a', '2026-11-19T10:00:00Z')], note('post.removed', 'a'));
    expect(result.posts).toEqual([]);
    expect(result.refetch).toBe(false);
  });

  it('refetches for unknown new posts but not for posts already shown', () => {
    const feed = [post('a', '2026-11-19T10:00:00Z')];
    expect(applyNotification(feed, note('post.created', 'b')).refetch).toBe(true);
    expect(applyNotification(feed, note('post.created', 'a')).refetch).toBe(false);
  });

  it("toasts other people's new posts only", () => {
    expect(shouldToast(note('post.created', 'x', 'u-b'), 'u-a')).toBe(true);
    expect(shouldToast(note('post.created', 'x', 'u-a'), 'u-a')).toBe(false);
    expect(shouldToast(note('post.removed', 'x', 'u-b'), 'u-a')).toBe(false);
  });
});

describe('validateBody', () => {
  it('matches the server rule (3 to 1000 characters after trimming)', () => {
    expect(validateBody('  hi ')).not.toBeNull();
    expect(validateBody('Bins out')).toBeNull();
    expect(validateBody('x'.repeat(1001))).not.toBeNull();
  });
});
