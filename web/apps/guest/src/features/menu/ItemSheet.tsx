import type { PublicMenuItemResponse } from '@armenu/api-client';
import { useEffect, useRef } from 'react';

import { useI18n } from '../../i18n/i18n-context.ts';
import { formatPrice } from '@armenu/locale';
import { Badge } from '../../ui/Badge.tsx';
import { cx } from '../../ui/cx.ts';
import { CloseIcon } from '../../ui/icons.tsx';
import { ArStage } from '../ar/ArStage.tsx';

interface ItemSheetProps {
  /** The open dish; undefined closes the sheet. */
  readonly item: PublicMenuItemResponse | undefined;
  readonly currency: string;
  readonly onClose: () => void;
}

/**
 * Dish details in a native modal <dialog>: focus trapping, Escape, inert background and the top layer come from the
 * browser instead of JavaScript. A bottom sheet on phones, a centered card on larger screens.
 */
export function ItemSheet({ item, currency, onClose }: ItemSheetProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const isOpen = item !== undefined;

  useEffect(() => {
    const dialog = dialogRef.current;
    if (dialog === null) {
      return;
    }

    if (isOpen && !dialog.open) {
      dialog.showModal();
    } else if (!isOpen && dialog.open) {
      dialog.close();
    }
  }, [isOpen]);

  return (
    // Backdrop clicks are a pointer shortcut; keyboards close the sheet with Escape or the close button.
    // eslint-disable-next-line jsx-a11y/click-events-have-key-events, jsx-a11y/no-noninteractive-element-interactions
    <dialog
      ref={dialogRef}
      aria-labelledby="item-sheet-title"
      // Escape (or the browser's own dismissal) closed the dialog: bring the URL in line. When the URL change closed
      // it, there is no item any more and nothing to do.
      onClose={() => {
        if (isOpen) {
          onClose();
        }
      }}
      onClick={(event) => {
        // A click on the dialog element itself, not its content, is a click on the backdrop.
        if (event.target === event.currentTarget) {
          onClose();
        }
      }}
      className="m-0 mt-auto max-h-[92dvh] w-full max-w-none overflow-y-auto overscroll-contain rounded-t-3xl bg-surface text-ink shadow-2xl transition-[translate,opacity] duration-300 ease-out backdrop:bg-black/50 sm:m-auto sm:max-w-lg sm:rounded-3xl starting:open:translate-y-10 starting:open:opacity-0"
    >
      {item !== undefined && <ItemDetails item={item} currency={currency} onClose={onClose} />}
    </dialog>
  );
}

function ItemDetails({
  item,
  currency,
  onClose,
}: {
  item: PublicMenuItemResponse;
  currency: string;
  onClose: () => void;
}) {
  const { messages, culture } = useI18n();

  return (
    <article>
      <button
        type="button"
        onClick={onClose}
        aria-label={messages.close}
        className="absolute end-3 top-3 z-10 grid size-11 place-items-center rounded-full bg-surface/90 text-ink shadow-md backdrop-blur"
      >
        <CloseIcon className="size-5" />
      </button>

      {item.arModel !== null && <ArStage model={item.arModel} itemId={item.id} dishName={item.name} />}

      <div className={cx('px-6 pb-8', item.arModel === null ? 'pt-6' : 'pt-4')}>
        <div className={cx('flex items-start justify-between gap-4', item.arModel === null && 'pe-12')}>
          <h2 id="item-sheet-title" className="font-serif text-2xl leading-tight text-balance">
            {item.name}
          </h2>
          <p className="shrink-0 pt-1 text-lg font-semibold tabular-nums">
            {formatPrice(item.price, currency, culture)}
          </p>
        </div>
        {!item.isAvailable && (
          <p className="mt-3">
            <Badge tone="muted">{messages.soldOut}</Badge>
          </p>
        )}
        {item.description !== null && (
          <p className="mt-3 leading-relaxed text-ink-muted">{item.description}</p>
        )}
        {item.dietaryLabels.length > 0 && (
          <p className="mt-4 flex flex-wrap gap-2">
            {item.dietaryLabels.map((label) => (
              <Badge key={label} tone="accent">
                {messages.dietaryLabelNames[label] ?? label}
              </Badge>
            ))}
          </p>
        )}
        <section aria-labelledby="item-sheet-allergens" className="mt-5 rounded-2xl bg-stage px-4 py-3">
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
    </article>
  );
}
