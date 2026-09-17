import { describe, expect, test, vi } from 'vitest';

import { refreshMarginMs, Session, type SessionTransport, type TokenGrant } from './session.ts';

function setup(refreshResults: (TokenGrant | undefined)[] = []) {
  let now = 1_000_000;
  const transport = {
    signIn: vi.fn(() => Promise.resolve({ accessToken: 'signed-in', expiresIn: 900 })),
    refresh: vi.fn(() => Promise.resolve(refreshResults.shift())),
    signOut: vi.fn(() => Promise.resolve()),
  } satisfies SessionTransport;

  const session = new Session('cafe', transport, { now: () => now, lock: (_name, task) => task() });
  return { session, transport, advance: (ms: number) => (now += ms) };
}

describe('Session', () => {
  test('reuses the token until it is about to expire, then refreshes it', async () => {
    const { session, transport, advance } = setup([{ accessToken: 'refreshed', expiresIn: 900 }]);
    await session.signIn('owner@cafe.test', 'correct-horse-battery');

    advance(900_000 - refreshMarginMs - 1);
    expect(await session.accessToken()).toBe('signed-in');
    expect(transport.refresh).not.toHaveBeenCalled();

    advance(2);
    expect(await session.accessToken()).toBe('refreshed');
    expect(transport.refresh).toHaveBeenCalledOnce();
  });

  test('renewing fetches a token with what changed, without signing anyone out', async () => {
    const { session, transport } = setup([{ accessToken: 'with-verified-address', expiresIn: 900 }]);
    await session.signIn('owner@cafe.test', 'correct-horse-battery');
    const listener = vi.fn();
    session.subscribe(listener);

    expect(await session.renew()).toBe('with-verified-address');
    expect(transport.refresh).toHaveBeenCalledOnce();
    expect(session.isSignedIn).toBe(true);
    expect(listener).toHaveBeenCalledOnce();
  });

  test('concurrent requests share one refresh, so the rotating cookie is presented only once', async () => {
    const { session, transport } = setup([{ accessToken: 'restored', expiresIn: 900 }]);

    const tokens = await Promise.all([session.accessToken(), session.accessToken(), session.accessToken()]);

    expect(tokens).toEqual(['restored', 'restored', 'restored']);
    expect(transport.refresh).toHaveBeenCalledOnce();
  });

  test('ends when there is no session to refresh, and tells subscribers', async () => {
    const { session } = setup([undefined]);
    session.accept({ accessToken: 'about-to-expire', expiresIn: 30 });
    const listener = vi.fn();
    session.subscribe(listener);

    expect(await session.accessToken()).toBeUndefined();
    expect(session.isSignedIn).toBe(false);
    expect(listener).toHaveBeenCalledOnce();
  });

  test('a network failure during refresh does not sign the user out', async () => {
    const { session, transport } = setup();
    session.accept({ accessToken: 'about-to-expire', expiresIn: 30 });
    transport.refresh.mockRejectedValueOnce(new TypeError('Failed to fetch'));

    await expect(session.accessToken()).rejects.toThrow('Failed to fetch');
    expect(session.isSignedIn).toBe(true);
  });

  test('signing out forgets the token before revoking the session', async () => {
    const { session, transport } = setup();
    await session.signIn('owner@cafe.test', 'correct-horse-battery');
    transport.signOut.mockImplementationOnce(() => {
      expect(session.isSignedIn).toBe(false);
      return Promise.resolve();
    });

    await session.signOut();

    expect(transport.signOut).toHaveBeenCalledOnce();
  });
});
