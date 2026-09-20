import { m } from 'motion/react';
import type { ReactNode } from 'react';

import { cx } from '../cx.ts';
import { spring } from '../motion.ts';
import type { GlassSurface } from './GlassPanel.tsx';

export interface GlassButtonProps {
  readonly onPress: () => void;
  readonly surface?: GlassSurface;
  /** `icon` is a circle sized for a thumb; `pill` carries a label beside the icon. */
  readonly shape?: 'icon' | 'pill';
  /** Fills with the business's own colour. For the one action a screen is really about. */
  readonly emphasis?: 'plain' | 'brand';
  /**
   * Required for `icon`, which has no visible text. For `pill` it overrides the label only when the visible words
   * are not the whole story ("Reset" → "Reset the view").
   */
  readonly label?: string;
  readonly isDisabled?: boolean;
  /** Reflected as `aria-pressed`: for controls that stay on, like auto-rotate. */
  readonly isPressed?: boolean;
  readonly className?: string | undefined;
  readonly children?: ReactNode;
}

/**
 * A floating control.
 *
 * The press animation is a scale on the *whole* control rather than a background change, because these sit on
 * glass: a hover tint is invisible over a busy photograph, while a 4 % squash reads on anything. `whileTap` is
 * pointer-driven and covers touch, which has no hover state to rely on at all.
 *
 * Framer Motion's `whileTap` is a visual affordance only — the click handler is React's, so keyboard activation,
 * Enter/Space and the accessibility tree all behave exactly as a plain `<button>` does.
 */
export function GlassButton({
  onPress,
  surface = 'page',
  shape = 'icon',
  emphasis = 'plain',
  label,
  isDisabled = false,
  isPressed,
  className,
  children,
}: GlassButtonProps) {
  const isBrand = emphasis === 'brand';

  return (
    <m.button
      type="button"
      onClick={onPress}
      disabled={isDisabled}
      aria-label={label}
      aria-pressed={isPressed}
      data-over-media={surface === 'media' && !isBrand ? '' : undefined}
      // A disabled control gets `pointer-events-none` below, so it never receives the tap this responds to.
      whileTap={{ scale: 0.96 }}
      transition={spring.press}
      className={cx(
        'inline-flex shrink-0 items-center justify-center gap-2 font-semibold select-none',
        // 44 px is the smallest reliable thumb target, and this UI is used one-handed while holding a phone.
        shape === 'icon' ? 'size-11 rounded-full' : 'h-11 rounded-full px-5 text-sm',
        isBrand
          ? 'bg-brand text-brand-ink shadow-float'
          : surface === 'media'
            ? 'rounded-full glass-over-media'
            : 'rounded-full glass text-ink shadow-panel',
        // An "on" toggle needs to read as on without colour alone: the ring is the second channel.
        isPressed === true && !isBrand && 'ring-2 ring-accent ring-offset-0',
        isDisabled && 'pointer-events-none opacity-40',
        className,
      )}
    >
      {children}
    </m.button>
  );
}
