import type { PublicMenuResponse } from '@armenu/api-client';

import { fetchPublicMenu } from './menu-api.ts';
import { isSameMenuRequest, type MenuRequest, menuRequestFromUrl } from './menu-request.ts';

let preloaded: { readonly request: MenuRequest; readonly response: Promise<PublicMenuResponse> } | undefined;

/**
 * Starts the request for the menu in the address bar while the application code is still downloading, instead of
 * after React has started. On a phone this saves the round trip that dominates the time to a readable menu.
 */
export function startMenuPreload(location: Location): void {
  const request = menuRequestFromUrl(new URL(location.href), import.meta.env.BASE_URL);
  if (request === undefined) {
    return;
  }

  const response = fetchPublicMenu(request);
  // Failures surface when the application takes the response; until then the rejection is expected, not unhandled.
  response.catch(() => undefined);
  preloaded = { request, response };
}

/** Hands the preloaded response over once, and only for the request it was made for. */
export function takePreloadedMenu(request: MenuRequest): Promise<PublicMenuResponse> | undefined {
  if (preloaded === undefined || !isSameMenuRequest(preloaded.request, request)) {
    return undefined;
  }

  const { response } = preloaded;
  preloaded = undefined;
  return response;
}
