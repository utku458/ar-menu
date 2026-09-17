import { createApiClient } from '@armenu/api-client';
import { createContext, use } from 'react';

import { env } from '../../env.ts';
import { type MenuEvent, MenuEventTracker } from './menu-events.ts';

const api = createApiClient(env.apiBaseUrl);
const trackers = new Map<string, MenuEventTracker>();

/** The tracker of a menu, shared by everything on the page; batches still pending go out when the page is hidden. */
export function trackerFor(tenant: string): MenuEventTracker {
  let tracker = trackers.get(tenant);
  if (tracker === undefined) {
    tracker = new MenuEventTracker((events) => {
      // keepalive: the request survives the page being closed right after a dish was opened.
      void api
        .POST('/api/v1/menus/{tenant}/events', {
          params: { path: { tenant } },
          body: { events: [...events] },
          keepalive: true,
        })
        .catch(() => undefined);
    });
    trackers.set(tenant, tracker);
  }

  return tracker;
}

if (typeof document !== 'undefined') {
  const flushAll = () => {
    for (const tracker of trackers.values()) {
      tracker.flush();
    }
  };
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'hidden') {
      flushAll();
    }
  });
  window.addEventListener('pagehide', flushAll);
}

export type TrackMenuEvent = (event: MenuEvent) => void;

export const MenuEventsContext = createContext<TrackMenuEvent>(() => undefined);

export function useTrackMenuEvent(): TrackMenuEvent {
  return use(MenuEventsContext);
}
