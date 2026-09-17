export type WebApp = 'guest' | 'dashboard';

/** The build variables (`VITE_*`) an app is configured with; the origins in its policy come from them. */
export type BuildEnv = Readonly<Record<string, string | undefined>>;

type Directives = Readonly<Record<string, readonly string[]>>;

/**
 * The response headers of a web app's pages, derived from the same variables its build uses, so the policy always
 * names exactly the API and storage the bundle talks to.
 *
 * The one source of truth for two servers: `vite preview` (every end-to-end test runs under these headers) and the
 * nginx image (see nginx-headers.ts).
 */
export function securityHeaders(app: WebApp, env: BuildEnv): Record<string, string> {
  return {
    'Content-Security-Policy': serialize(app === 'guest' ? guestPolicy(env) : dashboardPolicy(env)),
    'X-Content-Type-Options': 'nosniff',
    'Referrer-Policy': app === 'guest' ? 'strict-origin-when-cross-origin' : 'no-referrer',
    'Cross-Origin-Opener-Policy': 'same-origin',
    // WebXR sessions (AR on Android) are the only powerful feature either app uses.
    'Permissions-Policy':
      'xr-spatial-tracking=(self), camera=(), microphone=(), geolocation=(), payment=(), usb=()',
  };
}

/** Parts both apps share: nothing framed, no plugins, no foreign base URL or form target. */
const common: Directives = {
  'default-src': ["'self'"],
  'base-uri': ["'none'"],
  'object-src': ["'none'"],
  'frame-ancestors': ["'none'"],
  'form-action': ["'self'"],
  'font-src': ["'self'"],
  'manifest-src': ["'self'"],
};

/**
 * The 3D viewer (`<model-viewer>`, three.js): WebAssembly for the Meshopt decoder, an empty in-memory script that
 * switches the decoder on, textures and models read as blob and data URLs.
 */
const viewer: Directives = {
  'script-src': ["'self'", "'wasm-unsafe-eval'", 'blob:'],
  'worker-src': ["'self'", 'blob:'],
  // React style attributes and the viewer's shadow DOM styles.
  'style-src': ["'self'", "'unsafe-inline'"],
};

function guestPolicy(env: BuildEnv): Directives {
  const api = origin(env, 'VITE_API_BASE_URL');
  const assets = origin(env, 'VITE_ASSETS_ORIGIN');

  return {
    ...common,
    ...viewer,
    'connect-src': ["'self'", api, assets, 'blob:', 'data:'],
    'img-src': ["'self'", assets, 'blob:', 'data:'],
    'media-src': ["'self'", assets],
  };
}

function dashboardPolicy(env: BuildEnv): Directives {
  const api = origin(env, 'VITE_API_BASE_URL');
  // Uploads go straight to storage through presigned URLs; previews and posters come from the assets origin.
  const storage = origins(env, 'VITE_STORAGE_ORIGINS');

  return {
    ...common,
    ...viewer,
    'connect-src': ["'self'", api, ...storage, 'blob:', 'data:'],
    'img-src': ["'self'", ...storage, 'blob:', 'data:'],
  };
}

function serialize(directives: Directives): string {
  return Object.entries(directives)
    .map(([name, sources]) => [name, ...new Set(sources)].join(' '))
    .join('; ');
}

function origin(env: BuildEnv, name: string): string {
  const value = env[name]?.trim();
  if (value === undefined || value === '') {
    throw new Error(`${name} is required to build the content security policy.`);
  }

  let url: URL;
  try {
    url = new URL(value);
  } catch {
    throw new Error(`${name} must be an absolute URL, not '${value}'.`);
  }

  if (url.protocol !== 'https:' && url.protocol !== 'http:') {
    throw new Error(`${name} must be an http(s) URL, not '${value}'.`);
  }

  return url.origin;
}

function origins(env: BuildEnv, name: string): string[] {
  const values = (env[name] ?? '').split(/[\s,]+/).filter((value) => value !== '');
  if (values.length === 0) {
    throw new Error(`${name} is required to build the content security policy.`);
  }

  return values.map((value, index) => origin({ [`${name}[${index}]`]: value }, `${name}[${index}]`));
}
