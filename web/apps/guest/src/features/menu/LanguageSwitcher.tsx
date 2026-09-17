import { useI18n } from '../../i18n/i18n-context.ts';
import { languageName } from '@armenu/locale';
import { ChevronDownIcon, GlobeIcon } from '../../ui/icons.tsx';

interface LanguageSwitcherProps {
  readonly cultures: readonly string[];
  readonly value: string;
  readonly onChange: (culture: string) => void;
  readonly isBusy: boolean;
}

/** A native select: the platform picker is the most familiar and accessible choice on phones, at no bundle cost. */
export function LanguageSwitcher({ cultures, value, onChange, isBusy }: LanguageSwitcherProps) {
  const { messages } = useI18n();

  return (
    <label className="relative inline-flex shrink-0 items-center text-ink">
      <span className="sr-only">{messages.language}</span>
      <GlobeIcon className="pointer-events-none absolute start-3 size-4 text-ink-muted" />
      <select
        value={value}
        aria-busy={isBusy}
        onChange={(event) => {
          onChange(event.target.value);
        }}
        className="h-11 appearance-none rounded-full border border-line bg-surface ps-9 pe-9 text-sm font-medium"
      >
        {cultures.map((culture) => (
          <option key={culture} value={culture} lang={culture}>
            {languageName(culture)}
          </option>
        ))}
      </select>
      <ChevronDownIcon className="pointer-events-none absolute end-3 size-4 text-ink-muted" />
    </label>
  );
}
