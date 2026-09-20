/// <reference path="./model-viewer-jsx.d.ts" />
import { ModelViewerElement } from '@google/model-viewer';
import { type ReactNode, type Ref, use, useEffect, useEffectEvent, useImperativeHandle, useRef } from 'react';

import { viewerPreset } from './preset.ts';

// Processed models use Meshopt geometry. <model-viewer> bundles three.js's Meshopt decoder but only enables it after
// loading a decoder script from a URL; an empty in-memory script satisfies that without downloading the decoder twice,
// and without a request to a third-party CDN.
ModelViewerElement.meshoptDecoderLocation = URL.createObjectURL(new Blob([''], { type: 'text/javascript' }));

let sceneViewerDevice: Promise<boolean> | undefined;

/**
 * Whether AR on this device would open Android Scene Viewer, which loads the model file itself and decodes no
 * compression extensions. <model-viewer> prefers WebXR (three.js, which decodes everything) wherever it is supported,
 * and falls back to Scene Viewer on other Android browsers.
 */
function isSceneViewerDevice(): Promise<boolean> {
  sceneViewerDevice ??= (async () => {
    if (!/android/i.test(navigator.userAgent) || /firefox|oculus/i.test(navigator.userAgent)) {
      return false;
    }
    try {
      return !(await navigator.xr?.isSessionSupported('immersive-ar'));
    } catch {
      return true;
    }
  })();
  return sceneViewerDevice;
}

export type ArStatus = 'not-presenting' | 'session-started' | 'object-placed' | 'failed';

export interface ArViewerLoadDetails {
  readonly canActivateAR: boolean;
}

/**
 * What the surrounding interface may do to the viewer.
 *
 * Deliberately narrow. Exposing the `<model-viewer>` element itself would let any consumer reach for its ~80
 * attributes and quietly couple the app to the library, so this package hands out the four verbs the product
 * actually needs and keeps the element private. Swapping the 3D implementation later is then a change inside this
 * package rather than a search across the apps.
 */
export interface ArViewerControls {
  /** Returns the dish to the framing every poster was rendered at. */
  resetView(): void;
  /** Turns the dish slowly on its own, as a display would. */
  setAutoRotate(isRotating: boolean): void;
  /** Starts augmented reality. Rejects when the device or the session refuses. */
  activateAr(): Promise<void>;
  /** False on desktops and anywhere without an AR mode. */
  canActivateAr(): boolean;
}

export interface ArViewerProps {
  /** Binary glTF for the in-page 3D view and WebXR. */
  readonly src: string;
  /**
   * Plain binary glTF for devices whose AR opens Android Scene Viewer. Those devices load it instead of `src`, so the
   * page and AR show the same file.
   */
  readonly sceneViewerSrc?: string | undefined;
  /** USDZ for iOS AR Quick Look. Without it, <model-viewer> converts the GLB on the device. */
  readonly iosSrc?: string | undefined;
  /** Shown until the model has rendered its first frame. */
  readonly poster?: string | undefined;
  /** Accessible description of the model. */
  readonly alt: string;
  readonly className?: string | undefined;
  /** Receives the control surface once the viewer is mounted, for overlay buttons outside the element. */
  readonly controlsRef?: Ref<ArViewerControls | null>;
  /** Slotted content, e.g. `<button slot="ar-button">` and `<div slot="progress-bar">`. */
  readonly children?: ReactNode;
  readonly onProgress?: (progress: number) => void;
  /** Fires when the model is ready. `canActivateAR` is false on devices without an AR mode (desktops). */
  readonly onLoad?: (details: ArViewerLoadDetails) => void;
  readonly onError?: () => void;
  readonly onArStatus?: (status: ArStatus) => void;
}

/**
 * A dish in 3D and in augmented reality. Importing this module registers <model-viewer> (with three.js), so it is
 * meant to be loaded lazily, only when a guest opens a dish that has a model. It suspends for a moment on Android while
 * it asks the browser whether WebXR is available, to pick the model file.
 */
export function ArViewer({
  src,
  sceneViewerSrc,
  iosSrc,
  poster,
  alt,
  className,
  controlsRef,
  children,
  onProgress,
  onLoad,
  onError,
  onArStatus,
}: ArViewerProps) {
  const viewerRef = useRef<ModelViewerElement>(null);
  const modelSrc = sceneViewerSrc !== undefined && use(isSceneViewerDevice()) ? sceneViewerSrc : src;

  useImperativeHandle(
    controlsRef,
    (): ArViewerControls => ({
      resetView() {
        const viewer = viewerRef.current;
        if (viewer !== null) {
          // Both halves matter: the orbit puts the camera back, and the turntable reset undoes accumulated spin.
          viewer.cameraOrbit = viewerPreset.cameraOrbit;
          viewer.resetTurntableRotation();
        }
      },
      setAutoRotate(isRotating) {
        const viewer = viewerRef.current;
        if (viewer !== null) {
          viewer.autoRotate = isRotating;
        }
      },
      async activateAr() {
        await viewerRef.current?.activateAR();
      },
      canActivateAr() {
        return viewerRef.current?.canActivateAR ?? false;
      },
    }),
    [],
  );

  const handleProgress = useEffectEvent((event: Event) => {
    onProgress?.((event as CustomEvent<{ totalProgress: number }>).detail.totalProgress);
  });
  const handleLoad = useEffectEvent(() => {
    const viewer = viewerRef.current;
    if (viewer === null) {
      return;
    }

    onLoad?.({ canActivateAR: viewer.canActivateAR });
  });
  const handleError = useEffectEvent(() => {
    onError?.();
  });
  const handleArStatus = useEffectEvent((event: Event) => {
    onArStatus?.((event as CustomEvent<{ status: ArStatus }>).detail.status);
  });

  useEffect(() => {
    const viewer = viewerRef.current;
    if (viewer === null) {
      return;
    }

    const listeners: [string, (event: Event) => void][] = [
      ['progress', handleProgress],
      ['load', handleLoad],
      ['error', handleError],
      ['ar-status', handleArStatus],
    ];
    for (const [type, listener] of listeners) {
      viewer.addEventListener(type, listener);
    }

    return () => {
      for (const [type, listener] of listeners) {
        viewer.removeEventListener(type, listener);
      }
    };
  }, []);

  return (
    <model-viewer
      ref={viewerRef}
      src={modelSrc}
      ios-src={iosSrc}
      poster={poster}
      alt={alt}
      className={className}
      // AR shows the real portion size on the table, so guests cannot scale it: that would defeat the purpose.
      ar
      ar-modes="webxr scene-viewer quick-look"
      ar-scale="fixed"
      ar-placement="floor"
      xr-environment
      camera-controls
      // Horizontal drags turn the dish; vertical drags still scroll the page.
      touch-action="pan-y"
      interaction-prompt="none"
      camera-orbit={viewerPreset.cameraOrbit}
      environment-image={viewerPreset.environmentImage}
      tone-mapping={viewerPreset.toneMapping}
      exposure={viewerPreset.exposure}
      shadow-intensity={viewerPreset.shadowIntensity}
      shadow-softness={viewerPreset.shadowSoftness}
      // Mounted only on demand, so there is nothing left to defer.
      loading="eager"
    >
      {children}
    </model-viewer>
  );
}
