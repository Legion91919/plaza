import type { EventSummary, Group, Me, ModerationEntry, Post, PostKind } from './types';

/** RFC 9457 problem from the API; `code` is the stable machine code (e.g. `not_a_member`). */
export class ApiError extends Error {
  constructor(public readonly status: number, public readonly code: string | undefined, message: string) {
    super(message);
    this.name = 'ApiError';
  }
}

/** Dev-only stand-in for sign-in: the API trusts this header in Development. OIDC replaces it later. */
export const DEMO_USER_HEADER = 'X-Demo-User';

export interface ApiClient {
  demoUsers(): Promise<Me[]>;
  me(): Promise<Me>;
  group(groupId: string): Promise<Group>;
  feed(groupId: string): Promise<Post[]>;
  post(groupId: string, body: string, kind: PostKind, idempotencyKey: string): Promise<Post>;
  remove(groupId: string, postId: string, reason: string): Promise<ModerationEntry>;
  events(groupId: string): Promise<EventSummary[]>;
}

export function createApiClient(userId: string, fetchFn: typeof fetch = (...a) => fetch(...a)): ApiClient {
  async function call<T>(path: string, init: RequestInit = {}): Promise<T> {
    const headers = new Headers(init.headers);
    headers.set(DEMO_USER_HEADER, userId);
    if (init.body) headers.set('Content-Type', 'application/json');
    const res = await fetchFn(path, { ...init, headers });
    if (!res.ok) {
      let code: string | undefined;
      let message = `Request failed (${res.status})`;
      try {
        const problem = await res.json();
        code = problem.code;
        message = problem.detail ?? problem.title ?? message;
      } catch {
        /* not JSON */
      }
      throw new ApiError(res.status, code, message);
    }
    return (await res.json()) as T;
  }

  const g = (id: string) => `/api/groups/${encodeURIComponent(id)}`;
  return {
    demoUsers: () => call('/api/demo/users'),
    me: () => call('/api/me'),
    group: (id) => call(g(id)),
    feed: (id) => call(`${g(id)}/posts`),
    post: (id, body, kind, idempotencyKey) =>
      call(`${g(id)}/posts`, { method: 'POST', body: JSON.stringify({ body, kind }), headers: { 'Idempotency-Key': idempotencyKey } }),
    remove: (id, postId, reason) =>
      call(`${g(id)}/posts/${encodeURIComponent(postId)}/removal`, { method: 'POST', body: JSON.stringify({ reason }) }),
    events: (id) => call(`${g(id)}/events`),
  };
}
