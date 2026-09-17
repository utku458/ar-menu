import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest';

import { savePreferredLanguage } from './language-preference.ts';
import { isSameMenuRequest, menuRequestFromUrl, toMenuRequest } from './menu-request.ts';

describe('menu requests', () => {
  beforeEach(() => {
    const storage = new Map<string, string>();
    vi.stubGlobal('localStorage', {
      getItem: (key: string) => storage.get(key) ?? null,
      setItem: (key: string, value: string) => storage.set(key, value),
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  test('reads the restaurant and the explicit language from a menu URL', () => {
    const request = menuRequestFromUrl(new URL('https://armenu.app/m/Kadikoy-Burger-Lab?lang=en'), '/m/');

    expect(request).toEqual({ tenant: 'kadikoy-burger-lab', lang: 'en' });
  });

  test('ignores URLs that are not a single menu', () => {
    expect(menuRequestFromUrl(new URL('https://armenu.app/m/'), '/m/')).toBeUndefined();
    expect(menuRequestFromUrl(new URL('https://armenu.app/m/a/b'), '/m/')).toBeUndefined();
    expect(menuRequestFromUrl(new URL('https://armenu.app/pricing'), '/m/')).toBeUndefined();
  });

  test('falls back to the language chosen on an earlier visit, then to the browser language', () => {
    expect(toMenuRequest('cafe', undefined).lang).toBeUndefined();

    savePreferredLanguage('de');

    expect(toMenuRequest('cafe', '').lang).toBe('de');
    expect(toMenuRequest('cafe', 'tr').lang).toBe('tr');
  });

  test('the same restaurant in another language is another request', () => {
    expect(isSameMenuRequest({ tenant: 'cafe', lang: 'tr' }, { tenant: 'cafe', lang: 'tr' })).toBe(true);
    expect(isSameMenuRequest({ tenant: 'cafe', lang: 'tr' }, { tenant: 'cafe', lang: undefined })).toBe(
      false,
    );
  });
});
