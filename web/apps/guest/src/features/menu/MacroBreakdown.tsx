import { m } from 'motion/react';

import { useI18n } from '../../i18n/i18n-context.ts';
import { spring } from '../../ui/motion.ts';
import { macroShares, type MacroKey, type Nutrition } from './nutrition.ts';

/** One token per macronutrient, so the bar and its figure below can never drift apart. */
const MACRO_COLOR: Readonly<Record<MacroKey, string>> = {
  protein: 'var(--color-accent)',
  carbohydrate: 'var(--color-signal)',
  fat: 'var(--color-ink-muted)',
};

export interface MacroBreakdownProps {
  readonly nutrition: Nutrition;
  /** Staggers the bars in after the sheet has settled, so the two animations do not compete. */
  readonly delay?: number;
}

/**
 * Energy and macronutrients for a dish.
 *
 * A single stacked bar rather than three rings or a donut. Rings are the fashionable choice and the wrong one here:
 * comparing arc lengths is measurably harder than comparing bar segments, a donut needs a legend to say which colour
 * is which, and none of it survives being 40 pixels tall on a phone. The bar is read left to right like the numbers
 * beneath it, and the numbers are the part a guest counting carbohydrates actually needs.
 *
 * Colour is never the only channel: every segment is also labelled with its name and grams underneath. The bar is
 * `aria-hidden` because it is a picture of the figures next to it — a screen reader gets the figures.
 */
export function MacroBreakdown({ nutrition, delay = 0 }: MacroBreakdownProps) {
  const { messages, culture } = useI18n();
  const shares = macroShares(nutrition);

  if (shares.length === 0) {
    return null;
  }

  const number = new Intl.NumberFormat(culture, { maximumFractionDigits: 0 });

  return (
    <section aria-labelledby="item-sheet-nutrition" className="mt-6">
      <div className="flex items-baseline justify-between gap-3">
        <h3 id="item-sheet-nutrition" className="text-sm font-semibold">
          {messages.nutrition}
        </h3>
        <p className="text-sm text-ink-muted">
          {nutrition.perServing ? messages.perServing : messages.perHundredGrams}
        </p>
      </div>

      <p className="mt-2 flex items-baseline gap-1.5">
        <span className="font-serif text-3xl tabular-nums">{number.format(nutrition.kcal)}</span>
        <span className="text-sm font-medium text-ink-muted">{messages.kcal}</span>
      </p>

      <div
        aria-hidden="true"
        className="mt-3 flex h-2.5 w-full gap-0.5 overflow-hidden rounded-full bg-stage"
      >
        {shares.map((share, index) => (
          <m.div
            key={share.key}
            initial={{ scaleX: 0 }}
            animate={{ scaleX: 1 }}
            transition={{ ...spring.precise, delay: delay + index * 0.06 }}
            style={{ flexGrow: share.energyShare, originX: 0, backgroundColor: MACRO_COLOR[share.key] }}
            className="h-full rounded-full"
          />
        ))}
      </div>

      <dl className="mt-3 grid grid-cols-3 gap-2">
        {shares.map((share) => (
          <div key={share.key} className="rounded-control bg-stage px-3 py-2">
            <dt className="flex items-center gap-1.5 text-xs font-medium text-ink-muted">
              <span
                aria-hidden="true"
                className="size-2 shrink-0 rounded-full"
                style={{ backgroundColor: MACRO_COLOR[share.key] }}
              />
              {messages.macroNames[share.key]}
            </dt>
            <dd className="mt-0.5 font-semibold tabular-nums">
              {messages.grams(number.format(share.grams))}
            </dd>
          </div>
        ))}
      </dl>
    </section>
  );
}
