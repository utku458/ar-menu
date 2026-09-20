import type { ElementType, ReactNode } from 'react';

import { cx } from '../cx.ts';

export type GlassSurface =
  /** Glass over the app's own background: picks up the page tint, follows the colour scheme. */
  | 'page'
  /** Glass over a photograph, a 3D render or a camera-like viewport: always dark, always white text. */
  | 'media';

export interface GlassPanelProps {
  readonly surface?: GlassSurface;
  /** Renders as something other than a `div` — `section`, `figcaption`, `aside` — without losing the styling. */
  readonly as?: ElementType;
  readonly className?: string | undefined;
  readonly children?: ReactNode;
}

/**
 * The frosted slab everything floating is made of.
 *
 * Two surfaces rather than a colour prop, because the choice is not decorative: over media, contrast has to come
 * from the panel itself (nothing is known about the pixels behind it), so that variant hard-codes a dark tint and
 * white text and marks itself with `data-over-media`, which the base stylesheet uses to switch focus rings to
 * white-on-dark. Over the page, the panel inherits the theme instead.
 */
export function GlassPanel({ surface = 'page', as: Element = 'div', className, children }: GlassPanelProps) {
  return (
    <Element
      data-over-media={surface === 'media' ? '' : undefined}
      className={cx(
        'rounded-panel',
        surface === 'media' ? 'glass-over-media' : 'glass text-ink shadow-panel',
        className,
      )}
    >
      {children}
    </Element>
  );
}
