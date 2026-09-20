/**
 * Which augmented-reality path this device will take, decided once per page.
 *
 * This is the honest shape of "camera access" in this app: the guest app never opens a camera itself — the content
 * security policy sets `camera=()` and grants only `xr-spatial-tracking` — so there is no permission of ours to ask
 * for and no video frames of ours to draw on. `<model-viewer>` hands the dish to one of three very different
 * environments, and the interface has to promise the guest the right one:
 *
 *   - `webxr`       the session runs *inside this page*: the browser asks for camera access with its own prompt,
 *                   and the page keeps a viewport it can overlay.
 *   - `scene-viewer` Android hands off to Google's Scene Viewer app; our page is left behind.
 *   - `quick-look`  iOS hands off to AR Quick Look, a native screen. Nothing of ours is visible during the session.
 *   - `unsupported` desktops and anything else: the dish stays a turntable, and we say so.
 *
 * Getting this wrong is not cosmetic. Promising "point at your table" and then disappearing into another app is the
 * difference between a product that feels considered and one that feels broken.
 */
export type ArPath = 'webxr' | 'scene-viewer' | 'quick-look' | 'unsupported';

let probe: Promise<ArPath> | undefined;

/** Whether this browser can open a USDZ in AR Quick Look — the feature test Apple documents for it. */
function supportsQuickLook(): boolean {
  const anchor = document.createElement('a');
  return anchor.relList.supports('ar');
}

async function supportsWebXr(): Promise<boolean> {
  try {
    return (await navigator.xr?.isSessionSupported('immersive-ar')) === true;
  } catch {
    // Permissions-Policy or a privacy setting can make the query itself throw; either way the answer is no.
    return false;
  }
}

/**
 * Resolves the AR path, memoised for the lifetime of the page.
 *
 * `isSessionSupported` is asynchronous and, on some Android builds, slow enough to notice, so callers should start
 * this early — the dish sheet does, on open — rather than when a thumb is already on the button.
 */
export function detectArPath(): Promise<ArPath> {
  probe ??= (async (): Promise<ArPath> => {
    if (await supportsWebXr()) {
      return 'webxr';
    }
    if (supportsQuickLook()) {
      return 'quick-look';
    }
    // Firefox and the Oculus browser report Android but never open Scene Viewer.
    if (/android/i.test(navigator.userAgent) && !/firefox|oculus/i.test(navigator.userAgent)) {
      return 'scene-viewer';
    }
    return 'unsupported';
  })();

  return probe;
}

/** Test seam: lets a unit test or a Playwright fixture pin the path without faking `navigator.xr`. */
export function setArPathForTesting(path: ArPath | undefined): void {
  probe = path === undefined ? undefined : Promise.resolve(path);
}
