import { readPreferredLanguage } from './language-preference.ts';

/** Identifies a menu response: the restaurant, and the language when the guest chose one. */
export interface MenuRequest {
  readonly tenant: string;
  /** Undefined lets the API pick from the browser's Accept-Language. */
  readonly lang: string | undefined;
}

/** An explicit `?lang=` wins; otherwise a language the guest chose on an earlier visit. */
export function toMenuRequest(tenant: string, langParam: string | null | undefined): MenuRequest {
  const explicit = langParam?.trim();
  return {
    tenant: tenant.toLowerCase(),
    lang: explicit === undefined || explicit === '' ? readPreferredLanguage() : explicit,
  };
}

/** The menu request of a menu URL (`/m/{tenant}`), or undefined for any other page. */
export function menuRequestFromUrl(url: URL, basePath: string): MenuRequest | undefined {
  if (!url.pathname.startsWith(basePath)) {
    return undefined;
  }

  const segments = url.pathname.slice(basePath.length).split('/').filter(Boolean);
  const [tenant] = segments;
  if (segments.length !== 1 || tenant === undefined) {
    return undefined;
  }

  return toMenuRequest(decodeURIComponent(tenant), url.searchParams.get('lang'));
}

export function isSameMenuRequest(a: MenuRequest, b: MenuRequest): boolean {
  return a.tenant === b.tenant && a.lang === b.lang;
}
