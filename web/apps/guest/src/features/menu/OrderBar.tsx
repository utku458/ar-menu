import { formatPrice } from '@armenu/locale';
import { m } from 'motion/react';

import { useI18n } from '../../i18n/i18n-context.ts';
import { spring } from '../../ui/motion.ts';

export interface OrderBarProps {
  readonly price: number;
  readonly currency: string;
  readonly isAvailable: boolean;
  /**
   * Places the order.
   *
   * Optional, and absent by default, because **this platform has no ordering API**: the public menu contract is
   * read-only. A button that looks like it orders food and does not is worse than no button, so when no handler is
   * supplied the bar degrades to what the app can honestly promise — the price, pinned where a thumb can see it
   * while reading the ingredients. Wire this up the day `POST /orders` exists and the button appears.
   */
  readonly onOrder?: (() => void) | undefined;
}

/**
 * The price, and the order action when there is one, pinned to the bottom of the sheet.
 *
 * Sticky rather than in the flow: the price is the question a guest returns to while reading down a long dish, and
 * scrolling back up to check it is the small friction that makes a menu feel like a document instead of a product.
 * `pb-[env(safe-area-inset-bottom)]` keeps it clear of the home indicator on a notched phone.
 */
export function OrderBar({ price, currency, isAvailable, onOrder }: OrderBarProps) {
  const { messages, culture } = useI18n();

  return (
    <div className="sticky bottom-0 z-10 mt-2 border-t border-line bg-surface/85 px-6 pt-3 pb-[max(0.75rem,env(safe-area-inset-bottom))] backdrop-blur-xl">
      <div className="flex items-center justify-between gap-4">
        <div className="min-w-0">
          <p className="text-eyebrow text-ink-muted">{messages.price}</p>
          <p className="font-serif text-2xl tabular-nums">{formatPrice(price, currency, culture)}</p>
        </div>

        {onOrder !== undefined && (
          <m.button
            type="button"
            onClick={onOrder}
            disabled={!isAvailable}
            // Sold out, the button is `disabled:pointer-events-none` and never receives the tap this responds to.
            whileTap={{ scale: 0.97 }}
            transition={spring.press}
            className="h-12 shrink-0 rounded-full bg-brand px-7 font-semibold text-brand-ink shadow-float disabled:pointer-events-none disabled:opacity-45"
          >
            {isAvailable ? messages.orderNow : messages.soldOut}
          </m.button>
        )}
      </div>
    </div>
  );
}
