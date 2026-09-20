import type { ArModelResponse } from '@armenu/api-client';
import type { ArViewerControls } from '@armenu/ar-viewer';
import { m } from 'motion/react';
import { lazy, Suspense, useEffect, useRef, useState } from 'react';

import { useI18n } from '../../i18n/i18n-context.ts';
import { useTrackMenuEvent } from '../analytics/menu-events-context.ts';
import { GlassButton } from '../../ui/glass/GlassButton.tsx';
import { CubeIcon } from '../../ui/icons.tsx';
import { spring } from '../../ui/motion.ts';
import { type ArPath, detectArPath } from './ar-capability.ts';
import { type ArLaunchPhase, ArLaunchOverlay } from './ArLaunchOverlay.tsx';
import { ArStageOverlay } from './ArStageOverlay.tsx';
import { loadArViewer, prefersReducedData } from './load-ar-viewer.ts';

const ArViewer = lazy(async () => ({ default: (await loadArViewer()).ArViewer }));

type ModelState =
  | { readonly phase: 'loading'; readonly progress: number }
  | { readonly phase: 'ready'; readonly canActivateAR: boolean }
  | { readonly phase: 'failed' };

/**
 * The 3D area of a dish, and every state around it.
 *
 * The poster — already cached from the menu list — shows at once; `<model-viewer>` and the model stream in behind it
 * and take over without a jump, because both are rendered with the same preset. Three layers sit on the stage, in
 * z-order: the viewer, the floating controls, and the launch overlay that covers both during an AR handoff.
 *
 * All of the AR *state* lives here rather than in the overlays, so the overlays stay presentational and testable,
 * and so the one place that can get the lifecycle wrong is one file.
 */
export function ArStage({
  model,
  itemId,
  dishName,
}: {
  model: ArModelResponse;
  itemId: string;
  dishName: string;
}) {
  const { messages } = useI18n();
  const track = useTrackMenuEvent();
  const controls = useRef<ArViewerControls | null>(null);

  const [isRequested, setIsRequested] = useState(() => !prefersReducedData());
  const [state, setState] = useState<ModelState>({ phase: 'loading', progress: 0 });
  const [launch, setLaunch] = useState<ArLaunchPhase>({ kind: 'idle' });
  const [path, setPath] = useState<ArPath>('unsupported');

  // The probe is memoised and can be slow on Android, so it runs while the model loads, not under a waiting thumb.
  useEffect(() => {
    let isMounted = true;
    void detectArPath().then((detected) => {
      if (isMounted) {
        setPath(detected);
      }
    });
    return () => {
      isMounted = false;
    };
  }, []);

  const activateAr = () => {
    track({ type: 'ar_started', itemId });
    setLaunch({ kind: 'handingOff' });
    controls.current?.activateAr().catch(() => {
      setLaunch({ kind: 'failed' });
    });
  };

  /**
   * `<model-viewer>`'s session lifecycle, translated into the phases the overlay speaks.
   *
   * `not-presenting` is the ambiguous one: it fires both when a session ends normally and when the guest backs out
   * of the permission prompt, so it always returns the stage to idle and never reports a failure — a guest who
   * changed their mind has not hit an error and should not be told they have.
   */
  const handleArStatus = (status: 'not-presenting' | 'session-started' | 'object-placed' | 'failed') => {
    setLaunch(
      status === 'failed'
        ? { kind: 'failed' }
        : status === 'session-started'
          ? { kind: 'searching' }
          : status === 'object-placed'
            ? { kind: 'placed' }
            : { kind: 'idle' },
    );
  };

  const poster =
    model.posterUrl === null ? null : (
      <img
        src={model.posterUrl}
        alt=""
        decoding="async"
        className="absolute inset-0 size-full object-contain"
      />
    );

  const caption =
    state.phase === 'failed'
      ? messages.modelFailed
      : state.phase === 'ready'
        ? state.canActivateAR
          ? messages.dragToTurn
          : messages.arOnPhone
        : null;

  return (
    <figure className="m-0">
      <div className="relative isolate mx-auto aspect-square w-full max-w-[60dvh] overflow-hidden bg-stage">
        {/* A soft radial pool under the dish: the stage reads as lit rather than as a flat grey box. */}
        <div
          aria-hidden="true"
          className="absolute inset-0 bg-[radial-gradient(ellipse_at_50%_58%,var(--color-surface),transparent_70%)] opacity-70"
        />

        {isRequested ? (
          <Suspense fallback={poster}>
            <ArViewer
              src={model.glbUrl}
              sceneViewerSrc={model.sceneViewerGlbUrl ?? undefined}
              iosSrc={model.usdzUrl ?? undefined}
              poster={model.posterUrl ?? undefined}
              alt={messages.modelAlt(dishName)}
              controlsRef={controls}
              className="block size-full"
              onProgress={(progress) => {
                setState((current) =>
                  current.phase === 'loading' ? { phase: 'loading', progress } : current,
                );
              }}
              onLoad={({ canActivateAR }) => {
                setState({ phase: 'ready', canActivateAR });
                track({ type: 'model_viewed', itemId });
              }}
              onError={() => {
                setState({ phase: 'failed' });
              }}
              onArStatus={handleArStatus}
            >
              {/*
                No `slot="ar-button"`. The overlay's own pill calls `activateAr()`, which is the same public method
                the slotted button would call and is equally a user gesture; keeping the slot as well would put two
                controls with the same accessible name on the stage, which is a duplicate for assistive technology
                and an ambiguous locator for the end-to-end tests.
              */}
              <progress
                slot="progress-bar"
                aria-label={messages.loadingModel}
                max={1}
                value={state.phase === 'loading' ? state.progress : 1}
                hidden={state.phase !== 'loading'}
                className="absolute inset-x-0 top-0 z-10 h-0.5 w-full appearance-none bg-transparent [&::-moz-progress-bar]:bg-accent [&::-webkit-progress-bar]:bg-transparent [&::-webkit-progress-value]:bg-accent"
              />
            </ArViewer>
          </Suspense>
        ) : (
          // Data-saver guests: nothing 3D is fetched until they ask for it by name.
          <>
            {poster}
            <m.div
              initial={{ opacity: 0, y: 12 }}
              animate={{ opacity: 1, y: 0 }}
              transition={spring.control}
              className="absolute inset-x-0 bottom-4 flex justify-center"
            >
              <GlassButton
                shape="pill"
                onPress={() => {
                  setIsRequested(true);
                }}
              >
                <CubeIcon className="size-5" />
                {messages.showIn3d}
              </GlassButton>
            </m.div>
          </>
        )}

        <ArStageOverlay
          controls={controls}
          canActivateAr={state.phase === 'ready' && state.canActivateAR}
          isReady={state.phase === 'ready'}
          isHidden={launch.kind !== 'idle' && launch.kind !== 'placed'}
          onActivateAr={activateAr}
        />

        <ArLaunchOverlay
          phase={launch}
          path={path}
          dishName={dishName}
          onDismiss={() => {
            setLaunch({ kind: 'idle' });
          }}
        />
      </div>

      <figcaption aria-live="polite" className="px-6 pt-3 text-sm text-ink-muted empty:hidden">
        {caption}
      </figcaption>
    </figure>
  );
}
