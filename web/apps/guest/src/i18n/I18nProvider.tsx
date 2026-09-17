import { type ReactNode, useEffect } from 'react';

import { I18nContext } from './i18n-context.ts';
import { textDirection } from '@armenu/locale';
import { messagesFor } from './messages.ts';

/** Provides the page language and applies it to the document (`lang`, `dir`), which screen readers rely on. */
export function I18nProvider({ culture, children }: { culture: string; children: ReactNode }) {
  const direction = textDirection(culture);

  useEffect(() => {
    document.documentElement.lang = culture;
    document.documentElement.dir = direction;
  }, [culture, direction]);

  return <I18nContext value={{ culture, direction, messages: messagesFor(culture) }}>{children}</I18nContext>;
}
