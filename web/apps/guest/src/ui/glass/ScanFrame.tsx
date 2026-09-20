import { cx } from '../cx.ts';

export interface ScanFrameProps {
  /**
   * `searching` sweeps and breathes — the device is looking for a surface. `locked` stops the sweep and snaps the
   * corners to the accent colour: the surface was found and the dish is about to land.
   */
  readonly state: 'searching' | 'locked';
  readonly className?: string | undefined;
}

const CORNERS = [
  { id: 'tl', path: 'M2 26V10a8 8 0 0 1 8-8h16', className: 'start-0 top-0' },
  { id: 'tr', path: 'M2 26V10a8 8 0 0 1 8-8h16', className: 'end-0 top-0 -scale-x-100' },
  { id: 'br', path: 'M2 26V10a8 8 0 0 1 8-8h16', className: 'end-0 bottom-0 -scale-100' },
  { id: 'bl', path: 'M2 26V10a8 8 0 0 1 8-8h16', className: 'start-0 bottom-0 -scale-y-100' },
] as const;

/**
 * The reticle shown while a device hunts for a surface: four glowing corner brackets and a beam that sweeps between
 * them.
 *
 * Purely decorative and marked `aria-hidden` — the state it depicts is announced as text by whatever renders it, so
 * a screen reader hears "Point at your table" rather than a description of a rectangle.
 *
 * Both animations move only `transform` and `opacity`, which the compositor can run without laying out or painting a
 * frame. That matters more here than anywhere else in the app: this is on screen at the exact moment the device is
 * also decoding a GLB and starting a camera pipeline, and a reticle that stutters reads as a device that cannot
 * cope. The sweep is a single translated element rather than an animated gradient position for the same reason.
 */
export function ScanFrame({ state, className }: ScanFrameProps) {
  const isLocked = state === 'locked';

  return (
    <div aria-hidden="true" className={cx('pointer-events-none absolute inset-0 overflow-hidden', className)}>
      <div
        className={cx(
          'absolute inset-[14%] transition-[opacity,scale] duration-500 ease-out',
          isLocked ? 'scale-95 opacity-100' : 'scale-100 opacity-90 motion-safe:animate-breathe',
        )}
      >
        {CORNERS.map((corner) => (
          <svg
            key={corner.id}
            viewBox="0 0 28 28"
            fill="none"
            className={cx(
              'absolute size-8 transition-colors duration-500',
              isLocked ? 'text-accent' : 'text-signal-glow',
              corner.className,
            )}
            style={{ filter: 'drop-shadow(0 0 6px currentColor)' }}
          >
            <path d={corner.path} stroke="currentColor" strokeWidth={2.5} strokeLinecap="round" />
          </svg>
        ))}

        {/*
          The sweeping element is the full height of the frame with the beam on its top edge, so the keyframes can
          travel it with `translateY(100%)` — a percentage transform resolves against the element's own box, which
          keeps the animation on the compositor at any frame size.
        */}
        {!isLocked && (
          <div className="absolute inset-x-2 inset-y-0 motion-safe:animate-sweep motion-reduce:hidden">
            <div
              className="h-px w-full bg-gradient-to-r from-transparent via-signal-glow to-transparent"
              style={{ boxShadow: '0 0 12px 1px var(--color-signal-glow)' }}
            />
          </div>
        )}
      </div>
    </div>
  );
}
