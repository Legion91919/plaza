import { ApiError, createApiClient, DEMO_USER_HEADER } from './client';

function fakeFetch(status: number, body: unknown) {
  const calls: { url: string; init: RequestInit }[] = [];
  const fn = (async (url: string, init: RequestInit) => {
    calls.push({ url, init });
    return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
  }) as unknown as typeof fetch;
  return { fn, calls };
}

describe('api client', () => {
  it('sends the demo user and the idempotency key when posting', async () => {
    const { fn, calls } = fakeFetch(201, { postId: 'p1' });
    await createApiClient('u-pieter', fn).post('lindehof', 'Bins out tonight', 'notice', 'key-123');

    const headers = new Headers(calls[0]!.init.headers);
    expect(calls[0]!.url).toBe('/api/groups/lindehof/posts');
    expect(headers.get(DEMO_USER_HEADER)).toBe('u-pieter');
    expect(headers.get('Idempotency-Key')).toBe('key-123');
    expect(JSON.parse(calls[0]!.init.body as string)).toEqual({ body: 'Bins out tonight', kind: 'notice' });
  });

  it('turns a 403 problem into an ApiError with the stable code', async () => {
    const { fn } = fakeFetch(403, { title: 'Forbidden', status: 403, detail: 'Only members of this group can read its feed.', code: 'not_a_member' });

    const error = await createApiClient('u-ayesha', fn).feed('lindehof').catch((e) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect(error.status).toBe(403);
    expect(error.code).toBe('not_a_member');
    expect(error.message).toBe('Only members of this group can read its feed.');
  });
});
