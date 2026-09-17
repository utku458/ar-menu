import { createContext, use } from 'react';

import type { InterfaceLanguage, Messages } from './messages.ts';

export interface I18n {
  readonly language: InterfaceLanguage;
  readonly messages: Messages;
  readonly setLanguage: (language: InterfaceLanguage) => void;
  /** A user-facing message for an API error code, falling back to the text the API sent. */
  readonly describeError: (code: string | undefined, fallback?: string) => string;
}

export const I18nContext = createContext<I18n | undefined>(undefined);

export function useI18n(): I18n {
  const i18n = use(I18nContext);
  if (i18n === undefined) {
    throw new Error('useI18n must be used inside an I18nProvider.');
  }

  return i18n;
}
