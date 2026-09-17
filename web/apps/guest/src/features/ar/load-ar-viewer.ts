/** The 3D stack (<model-viewer> and three.js) lives in its own chunk, fetched only for dishes with a model. */
export const loadArViewer = () => import('@armenu/ar-viewer');

/** Starts fetching the 3D chunk when a guest is about to open a dish with a model (hover, focus, touch). */
export function preloadArViewer(): void {
  // A failure here is retried when the sheet actually opens, where it is reported.
  loadArViewer().catch(() => undefined);
}

interface NetworkInformation {
  readonly saveData?: boolean;
}

/** Guests who asked their browser to save data load models only on request. */
export function prefersReducedData(): boolean {
  const connection = (navigator as Navigator & { connection?: NetworkInformation }).connection;
  return connection?.saveData === true || window.matchMedia('(prefers-reduced-data: reduce)').matches;
}
