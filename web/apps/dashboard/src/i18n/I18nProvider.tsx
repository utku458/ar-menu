import { type ReactNode, useEffect, useState } from 'react';
import { I18nProvider as AriaI18nProvider } from 'react-aria-components';

import { errorMessage } from './error-messages.ts';
import { I18nContext } from './i18n-context.ts';
import { type InterfaceLanguage, messagesFor } from './messages.ts';

const storageKey = 'armenu.dashboard.language';

function initialLanguage(): InterfaceLanguage {
  try {
    const stored = localStorage.getItem(storageKey);
    if (stored === 'tr' || stored === 'en') {
      return stored;
    }
  } catch {
    // Storage unavailable: fall back to the browser language.
  }

  return navigator.language.toLowerCase().startsWith('tr') ? 'tr' : 'en';
}

export function I18nProvider({ children }: { children: ReactNode }) {
  const [language, setLanguage] = useState(initialLanguage);
  const messages = messagesFor(language);

  useEffect(() => {
    document.documentElement.lang = language;
  }, [language]);

  const changeLanguage = (next: InterfaceLanguage) => {
    setLanguage(next);
    try {
      localStorage.setItem(storageKey, next);
    } catch {
      // Not remembering the choice is acceptable.
    }
  };

  return (
    <I18nContext
      value={{
        language,
        messages,
        setLanguage: changeLanguage,
        describeError: (code, fallback) =>
          errorMessage(code, language) ?? fallback ?? messages.unexpectedError,
      }}
    >
      {/* Number, date and currency inputs follow the interface language. */}
      <AriaI18nProvider locale={language === 'tr' ? 'tr-TR' : 'en-US'}>{children}</AriaI18nProvider>
    </I18nContext>
  );
}
