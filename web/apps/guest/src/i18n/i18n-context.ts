import { createContext, use, useEffect } from 'react';

import type { Messages } from './messages.ts';

export interface I18n {
  readonly culture: string;
  readonly direction: 'ltr' | 'rtl';
  readonly messages: Messages;
}

export const I18nContext = createContext<I18n | undefined>(undefined);

export function useI18n(): I18n {
  const i18n = use(I18nContext);
  if (i18n === undefined) {
    throw new Error('useI18n must be used inside an I18nProvider.');
  }

  return i18n;
}

export function useDocumentTitle(title: string | undefined): void {
  useEffect(() => {
    if (title !== undefined) {
      document.title = title;
    }
  }, [title]);
}
