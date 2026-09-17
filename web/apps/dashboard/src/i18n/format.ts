import type { InterfaceLanguage } from './messages.ts';

/** A calendar date in the interface language, such as "15 Eyl 2026" or "Sep 15, 2026". */
export function formatDate(iso: string, language: InterfaceLanguage): string {
  return new Intl.DateTimeFormat(language === 'tr' ? 'tr-TR' : 'en-US', { dateStyle: 'medium' }).format(
    new Date(iso),
  );
}
