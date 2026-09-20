import type { PublicMenuItemResponse } from '@armenu/api-client';

import { useI18n } from '../../i18n/i18n-context.ts';
import { formatPrice } from '@armenu/locale';
import { Badge } from '../../ui/Badge.tsx';
import { CubeIcon } from '../../ui/icons.tsx';
import { preloadArViewer } from '../ar/load-ar-viewer.ts';
import { preloadItemSheet } from './load-item-sheet.ts';

interface MenuItemCardProps {
  readonly item: PublicMenuItemResponse;
  readonly currency: string;
  readonly onOpen: (itemId: string) => void;
  /** Images near the top of the page load eagerly; the rest wait until they scroll near. */
  readonly isAboveTheFold: boolean;
}

export function MenuItemCard({ item, currency, onOpen, isAboveTheFold }: MenuItemCardProps) {
  const { messages, culture } = useI18n();
  const posterUrl = item.arModel?.posterUrl ?? null;
  // Hovering, focusing or touching a dish starts fetching the sheet (and, for a 3D dish, the 3D code) before the tap
  // completes. Both imports are memoised by the browser, so repeated warm-ups cost nothing.
  const warmUp = () => {
    preloadItemSheet();
    if (item.arModel !== null) {
      preloadArViewer();
    }
  };

  return (
    <li className="relative flex gap-4 rounded-2xl border border-line bg-surface p-4 transition-colors hover:border-ink-muted/40 has-focus-visible:outline-2 has-focus-visible:outline-offset-2 has-focus-visible:outline-accent">
      <div className="min-w-0 flex-1">
        <h3 className="leading-snug font-semibold">
          {/* The stretched button makes the whole card tappable while the heading stays a heading. */}
          <button
            type="button"
            onClick={() => {
              onOpen(item.id);
            }}
            onPointerEnter={warmUp}
            onTouchStart={warmUp}
            onFocus={warmUp}
            className="text-start outline-none after:absolute after:inset-0 after:rounded-2xl"
          >
            {item.name}
          </button>
        </h3>
        {item.description !== null && (
          <p className="mt-1 line-clamp-2 text-sm leading-relaxed text-ink-muted">{item.description}</p>
        )}
        <div className="mt-3 flex flex-wrap items-center gap-2">
          <span className="font-semibold tabular-nums">{formatPrice(item.price, currency, culture)}</span>
          {!item.isAvailable && <Badge tone="muted">{messages.soldOut}</Badge>}
          {item.dietaryLabels
            // A vegan dish is vegetarian too; saying both on a small card is noise.
            .filter((label) => label !== 'vegetarian' || !item.dietaryLabels.includes('vegan'))
            .map((label) => (
              <Badge key={label} tone="muted">
                {messages.dietaryLabelNames[label] ?? label}
              </Badge>
            ))}
          {item.arModel !== null && (
            <Badge tone="accent">
              <CubeIcon className="size-3.5" />
              <span aria-hidden="true">3D</span>
              <span className="sr-only">{messages.viewableIn3d}</span>
            </Badge>
          )}
        </div>
      </div>
      {posterUrl !== null && (
        <img
          src={posterUrl}
          alt=""
          width={88}
          height={88}
          loading={isAboveTheFold ? 'eager' : 'lazy'}
          decoding="async"
          className="size-22 shrink-0 self-center rounded-xl bg-stage object-contain"
        />
      )}
    </li>
  );
}
