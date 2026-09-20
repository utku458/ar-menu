import { prefersReducedData } from '../ar/load-ar-viewer.ts';

/**
 * The dish sheet — Framer Motion, the drag physics and the AR overlays — lives in its own chunk.
 *
 * Nobody sees it before tapping a dish, so it has no business in the JavaScript that stands between a QR scan and a
 * readable menu (the build fails when that exceeds its budget; see vite-plugins/initial-load.ts). It is fetched in
 * idle time once the menu is on screen, so it is almost always there before the first tap.
 */
export const loadItemSheet = () => import('./ItemSheet.tsx');

/** Starts fetching the sheet when a guest is about to open a dish (hover, focus, touch). */
export function preloadItemSheet(): void {
  // A failure here surfaces when the sheet actually opens, like the 3D chunk's.
  loadItemSheet().catch(() => undefined);
}

/**
 * Fetches the sheet once the browser has nothing better to do, and returns a cleanup that cancels it.
 *
 * Guests who asked their browser to save data are skipped: for them the sheet loads on intent instead, from the
 * hover/touch/focus warm-up on each dish card. Safari has no `requestIdleCallback`; a short timeout after the menu
 * has rendered is the closest equivalent there.
 */
export function preloadItemSheetWhenIdle(): () => void {
  if (prefersReducedData()) {
    return () => undefined;
  }

  if ('requestIdleCallback' in window) {
    const handle = window.requestIdleCallback(preloadItemSheet, { timeout: 3000 });
    return () => {
      window.cancelIdleCallback(handle);
    };
  }

  const handle = setTimeout(preloadItemSheet, 1500);
  return () => {
    clearTimeout(handle);
  };
}
