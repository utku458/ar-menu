import type { PublicMenuItemResponse } from '@armenu/api-client';

import { useI18n } from '../../i18n/i18n-context.ts';
import { Badge } from '../../ui/Badge.tsx';
import { cx } from '../../ui/cx.ts';
import { CloseIcon } from '../../ui/icons.tsx';
import { MotionFeatures } from '../../ui/MotionFeatures.tsx';
import { Sheet } from '../../ui/Sheet.tsx';
import { ArStage } from '../ar/ArStage.tsx';
import { MacroBreakdown } from './MacroBreakdown.tsx';
import type { Nutrition } from './nutrition.ts';
import { OrderBar } from './OrderBar.tsx';

const TITLE_ID = 'item-sheet-title';

export interface ItemSheetProps {
  /** The open dish; undefined closes the sheet. */
  readonly item: PublicMenuItemResponse | undefined;
  readonly currency: string;
  readonly onClose: () => void;
  /** Nutrition, once the API carries it (see nutrition.ts). The section is omitted until then. */
  readonly nutrition?: Nutrition | undefined;
  /** Supplied only where an ordering backend exists; see OrderBar. */
  readonly onOrder?: ((itemId: string) => void) | undefined;
}

/**
 * Dish details in a swipeable sheet.
 *
 * This component owns composition and nothing else: the modal mechanics live in `<Sheet>`, the 3D and AR lifecycle
 * in `<ArStage>`, the figures in `<MacroBreakdown>`, the price and call to action in `<OrderBar>`. Each of those is
 * independently renderable and independently testable, and this file reads as the layout of the screen — which is
 * the point, because the layout is the part that changes when the product does.
 *
 * `<MotionFeatures>` wraps here rather than at the app root on purpose. This module *is* the dish sheet's lazy chunk
 * (see load-item-sheet.ts), so Framer Motion, which nothing else in the app uses, never reaches the startup bundle.
 */
export function ItemSheet({ item, currency, onClose, nutrition, onOrder }: ItemSheetProps) {
  return (
    <MotionFeatures>
      <Sheet isOpen={item !== undefined} labelledBy={TITLE_ID} onClose={onClose}>
        {item !== undefined && (
          <ItemDetails
            item={item}
            currency={currency}
            onClose={onClose}
            nutrition={nutrition}
            onOrder={onOrder}
          />
        )}
      </Sheet>
    </MotionFeatures>
  );
}

function ItemDetails({
  item,
  currency,
  onClose,
  nutrition,
  onOrder,
}: {
  item: PublicMenuItemResponse;
  currency: string;
  onClose: () => void;
  nutrition: Nutrition | undefined;
  onOrder: ((itemId: string) => void) | undefined;
}) {
  const { messages } = useI18n();
  const hasModel = item.arModel !== null;

  return (
    <article className="flex min-h-0 flex-col">
      {/*
        Must stay the first focusable element in the sheet: `showModal()` focuses the dialog's first focusable
        descendant, so a keyboard or screen-reader guest starts at the way out rather than inside a 3D viewer. Over
        the stage it becomes a glass chip, because the pixels behind it are a dish, not a surface.
      */}
      <button
        type="button"
        onClick={onClose}
        aria-label={messages.close}
        data-over-media={hasModel ? '' : undefined}
        className={cx(
          'absolute end-4 top-4 z-30 grid size-10 place-items-center rounded-full',
          hasModel ? 'glass-over-media' : 'glass text-ink shadow-panel',
        )}
      >
        <CloseIcon className="size-5" />
      </button>

      {hasModel && item.arModel !== null && (
        <ArStage model={item.arModel} itemId={item.id} dishName={item.name} />
      )}

      {/*
        Not animated on its own. The text rides in with the sheet at full opacity: a separate fade would make the
        name, price and allergens — the reasons the sheet was opened — the last things on screen to become legible.
      */}
      <div className={cx('px-6 pb-2', hasModel ? 'pt-4' : 'pt-6')}>
        <h2 id={TITLE_ID} className={cx('text-title', !hasModel && 'pe-12')}>
          {item.name}
        </h2>

        {(item.dietaryLabels.length > 0 || !item.isAvailable) && (
          <p className="mt-3 flex flex-wrap gap-2">
            {!item.isAvailable && <Badge tone="muted">{messages.soldOut}</Badge>}
            {item.dietaryLabels.map((label) => (
              <Badge key={label} tone="accent">
                {messages.dietaryLabelNames[label] ?? label}
              </Badge>
            ))}
          </p>
        )}

        {item.description !== null && (
          <p className="mt-3 leading-relaxed text-ink-muted">{item.description}</p>
        )}

        {nutrition !== undefined && <MacroBreakdown nutrition={nutrition} delay={0.12} />}

        <section aria-labelledby="item-sheet-allergens" className="mt-6 rounded-panel bg-stage px-4 py-3">
          <h3 id="item-sheet-allergens" className="text-sm font-semibold">
            {messages.allergens}
          </h3>
          <p className="mt-1 text-sm leading-relaxed">
            {item.allergens === null
              ? messages.allergensNotDeclared
              : item.allergens.length === 0
                ? messages.containsNoAllergens
                : item.allergens.map((code) => messages.allergenNames[code] ?? code).join(', ')}
          </p>
          {item.allergens !== null && (
            <p className="mt-1 text-xs text-ink-muted">{messages.allergensDeclaredBy}</p>
          )}
        </section>
      </div>

      <OrderBar
        price={item.price}
        currency={currency}
        isAvailable={item.isAvailable}
        onOrder={
          onOrder === undefined
            ? undefined
            : () => {
                onOrder(item.id);
              }
        }
      />
    </article>
  );
}
