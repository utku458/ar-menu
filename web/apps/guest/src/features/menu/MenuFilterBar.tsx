import { useId, useState } from 'react';

import { useI18n } from '../../i18n/i18n-context.ts';
import { cx } from '../../ui/cx.ts';
import { SearchIcon, SlidersIcon } from '../../ui/icons.tsx';
import {
  allergenCodes,
  dietaryLabelCodes,
  type FilteredMenu,
  isFiltering,
  type MenuFilter,
  noFilter,
  toggled,
} from './menu-filter.ts';

interface MenuFilterBarProps {
  readonly filter: MenuFilter;
  readonly result: FilteredMenu;
  readonly onChange: (filter: MenuFilter) => void;
}

const chip =
  'rounded-full border border-line px-3 py-1.5 text-sm font-medium text-ink-muted transition-colors ' +
  'hover:text-ink aria-pressed:border-ink aria-pressed:bg-ink aria-pressed:text-canvas';

/**
 * Search, diets and allergens to avoid. The panel of choices stays folded until asked for, so a guest who only wants
 * to read the menu sees one search field, not twenty buttons.
 */
export function MenuFilterBar({ filter, result, onChange }: MenuFilterBarProps) {
  const { messages } = useI18n();
  const [isOpen, setIsOpen] = useState(false);
  const panelId = useId();
  const choices = filter.diets.length + filter.avoid.length;

  return (
    <div role="search" className="mx-auto max-w-2xl px-4 pb-3">
      <div className="flex gap-2">
        <label className="relative flex-1">
          <span className="sr-only">{messages.searchMenu}</span>
          <SearchIcon className="pointer-events-none absolute start-3 top-1/2 size-5 -translate-y-1/2 text-ink-muted" />
          <input
            type="search"
            value={filter.query}
            placeholder={messages.searchMenu}
            enterKeyHint="search"
            onChange={(event) => {
              onChange({ ...filter, query: event.target.value });
            }}
            className="h-11 w-full rounded-full border border-line bg-surface ps-10 pe-4 text-base outline-none placeholder:text-ink-muted focus-visible:border-accent"
          />
        </label>
        <button
          type="button"
          aria-expanded={isOpen}
          aria-controls={panelId}
          onClick={() => {
            setIsOpen((open) => !open);
          }}
          className={cx(
            'flex h-11 shrink-0 items-center gap-2 rounded-full border px-4 text-sm font-medium',
            choices > 0 ? 'border-ink bg-ink text-canvas' : 'border-line bg-surface text-ink',
          )}
        >
          <SlidersIcon className="size-5" />
          {messages.filters}
          {choices > 0 && <span className="tabular-nums">({choices})</span>}
        </button>
      </div>

      <div
        id={panelId}
        hidden={!isOpen}
        className="mt-3 flex flex-col gap-3 rounded-2xl border border-line bg-surface p-4"
      >
        <ChoiceGroup
          title={messages.suitableFor}
          codes={dietaryLabelCodes}
          names={messages.dietaryLabelNames}
          selected={filter.diets}
          onToggle={(code) => {
            onChange({ ...filter, diets: toggled(filter.diets, code) });
          }}
        />
        <ChoiceGroup
          title={messages.without}
          codes={allergenCodes}
          names={messages.allergenNames}
          selected={filter.avoid}
          onToggle={(code) => {
            onChange({ ...filter, avoid: toggled(filter.avoid, code) });
          }}
        />
      </div>

      {/* Announced as it changes, so a screen reader hears how many dishes are left after each choice. */}
      <div role="status" className="mt-3 empty:hidden">
        {isFiltering(filter) && (
          <div className="flex flex-wrap items-center gap-x-3 gap-y-1 px-1 text-sm">
            <span className="font-medium">
              {result.matches === 0 ? messages.noMatchingDishes : messages.resultCount(result.matches)}
            </span>
            <button
              type="button"
              onClick={() => {
                onChange(noFilter);
              }}
              className="font-medium text-accent underline underline-offset-2"
            >
              {messages.clearFilters}
            </button>
            {result.undeclared > 0 && (
              <p className="w-full text-ink-muted">{messages.undeclaredHidden(result.undeclared)}</p>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

interface ChoiceGroupProps {
  readonly title: string;
  readonly codes: readonly string[];
  readonly names: Readonly<Record<string, string>>;
  readonly selected: readonly string[];
  readonly onToggle: (code: string) => void;
}

function ChoiceGroup({ title, codes, names, selected, onToggle }: ChoiceGroupProps) {
  const titleId = useId();

  return (
    <div role="group" aria-labelledby={titleId}>
      <p
        id={titleId}
        className="mb-2 text-xs font-semibold tracking-wide text-ink-muted uppercase rtl:tracking-normal"
      >
        {title}
      </p>
      <div className="flex flex-wrap gap-2">
        {codes.map((code) => (
          <button
            key={code}
            type="button"
            aria-pressed={selected.includes(code)}
            onClick={() => {
              onToggle(code);
            }}
            className={chip}
          >
            {names[code] ?? code}
          </button>
        ))}
      </div>
    </div>
  );
}
