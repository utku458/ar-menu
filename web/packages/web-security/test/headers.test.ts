import { describe, expect, it } from 'vitest';

import { securityHeaders } from '../src/headers.ts';

const guestEnv = {
  VITE_API_BASE_URL: 'https://api.armenu.app/',
  VITE_ASSETS_ORIGIN: 'https://cdn.armenu.app',
};

function directives(policy: string | undefined): Record<string, string[]> {
  return Object.fromEntries(
    (policy ?? '').split('; ').map((directive) => {
      const [name = '', ...sources] = directive.split(' ');
      return [name, sources];
    }),
  );
}

describe('securityHeaders', () => {
  it('lets the guest menu reach only its API and asset origins', () => {
    const csp = directives(securityHeaders('guest', guestEnv)['Content-Security-Policy']);

    expect(csp['connect-src']).toEqual([
      "'self'",
      'https://api.armenu.app',
      'https://cdn.armenu.app',
      'blob:',
      'data:',
    ]);
    expect(csp['img-src']).toEqual(["'self'", 'https://cdn.armenu.app', 'blob:', 'data:']);
    expect(csp['script-src']).toEqual(["'self'", "'wasm-unsafe-eval'", 'blob:']);
    expect(csp['frame-ancestors']).toEqual(["'none'"]);
    expect(csp['object-src']).toEqual(["'none'"]);
    expect(Object.values(csp).flat()).not.toContain("'unsafe-eval'");
  });

  it('lets the dashboard upload to every storage origin it is built with', () => {
    const headers = securityHeaders('dashboard', {
      VITE_API_BASE_URL: 'https://api.armenu.app',
      VITE_STORAGE_ORIGINS: 'https://uploads.example.com, https://cdn.armenu.app/assets/',
    });

    expect(directives(headers['Content-Security-Policy'])['connect-src']).toEqual([
      "'self'",
      'https://api.armenu.app',
      'https://uploads.example.com',
      'https://cdn.armenu.app',
      'blob:',
      'data:',
    ]);
    expect(headers['Referrer-Policy']).toBe('no-referrer');
  });

  it('allows AR and nothing else powerful', () => {
    expect(securityHeaders('guest', guestEnv)['Permissions-Policy']).toMatch(
      /^xr-spatial-tracking=\(self\), camera=\(\)/,
    );
  });

  it.each([
    [{ VITE_ASSETS_ORIGIN: 'https://cdn.armenu.app' }, 'VITE_API_BASE_URL is required'],
    [{ ...guestEnv, VITE_ASSETS_ORIGIN: '*' }, "VITE_ASSETS_ORIGIN must be an absolute URL, not '*'"],
    [{ ...guestEnv, VITE_API_BASE_URL: "javascript:alert('x')" }, 'must be an http(s) URL'],
  ])('refuses to build a policy from %j', (env, message) => {
    expect(() => securityHeaders('guest', env)).toThrow(message);
  });
});
