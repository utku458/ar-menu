import type { ArModelResponse } from '@armenu/api-client';
import { lazy, Suspense, useState } from 'react';

import { useI18n } from '../../i18n/i18n-context.ts';
import { useTrackMenuEvent } from '../analytics/menu-events-context.ts';
import { CubeIcon } from '../../ui/icons.tsx';
import { loadArViewer, prefersReducedData } from './load-ar-viewer.ts';

const ArViewer = lazy(async () => ({ default: (await loadArViewer()).ArViewer }));

type ModelState =
  | { readonly phase: 'loading'; readonly progress: number }
  | { readonly phase: 'ready'; readonly canActivateAR: boolean }
  | { readonly phase: 'failed' };

/**
 * The 3D area of a dish. The poster (already cached from the menu list) shows at once; <model-viewer> and the model
 * stream in behind it and take over without a jump, because both are rendered with the same preset.
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
  const [isRequested, setIsRequested] = useState(() => !prefersReducedData());
  const [state, setState] = useState<ModelState>({ phase: 'loading', progress: 0 });
  const [arFailed, setArFailed] = useState(false);

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
      : arFailed
        ? messages.arFailed
        : state.phase === 'ready'
          ? state.canActivateAR
            ? messages.dragToTurn
            : messages.arOnPhone
          : null;

  return (
    <figure className="m-0">
      <div className="relative mx-auto aspect-square w-full max-w-[60dvh] overflow-hidden bg-stage">
        {isRequested ? (
          <Suspense fallback={poster}>
            <ArViewer
              src={model.glbUrl}
              sceneViewerSrc={model.sceneViewerGlbUrl ?? undefined}
              iosSrc={model.usdzUrl ?? undefined}
              poster={model.posterUrl ?? undefined}
              alt={messages.modelAlt(dishName)}
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
              onArStatus={(status) => {
                setArFailed(status === 'failed');
              }}
            >
              <button
                slot="ar-button"
                type="button"
                onClick={() => {
                  track({ type: 'ar_started', itemId });
                }}
                className="absolute inset-x-0 bottom-4 mx-auto flex h-12 w-fit items-center gap-2 rounded-full bg-brand px-6 font-semibold text-brand-ink shadow-lg"
              >
                <CubeIcon className="size-5" />
                {messages.seeOnYourTable}
              </button>
              <progress
                slot="progress-bar"
                aria-label={messages.loadingModel}
                max={1}
                value={state.phase === 'loading' ? state.progress : 1}
                hidden={state.phase !== 'loading'}
                className="absolute inset-x-0 top-0 h-1 w-full appearance-none bg-transparent [&::-moz-progress-bar]:bg-accent [&::-webkit-progress-bar]:bg-transparent [&::-webkit-progress-value]:bg-accent"
              />
            </ArViewer>
          </Suspense>
        ) : (
          <>
            {poster}
            <button
              type="button"
              onClick={() => {
                setIsRequested(true);
              }}
              className="absolute inset-x-0 bottom-4 mx-auto flex h-12 w-fit items-center gap-2 rounded-full bg-surface px-6 font-semibold text-ink shadow-lg"
            >
              <CubeIcon className="size-5" />
              {messages.showIn3d}
            </button>
          </>
        )}
      </div>
      <figcaption aria-live="polite" className="px-6 pt-3 text-sm text-ink-muted empty:hidden">
        {caption}
      </figcaption>
    </figure>
  );
}
