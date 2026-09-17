/**
 * How every dish is framed and lit. The in-page viewer and the poster renderer both use it, so the poster a guest
 * sees first is the first frame of the 3D model and nothing jumps when the model takes over.
 *
 * Framework-free on purpose: build tools import it without React or three.js.
 */
export const viewerPreset = {
  /** Three-quarter view from above: shows a burger's layers and a plate's contents. */
  cameraOrbit: '30deg 62deg auto',
  environmentImage: 'neutral',
  toneMapping: 'neutral',
  exposure: '1',
  shadowIntensity: '1',
  shadowSoftness: '0.9',
} as const;

/** Posters are square, like the viewer, and sized for a full-width sheet on a 3x phone screen without upscaling. */
export const posterSize = 768;
