import { formText } from '../../ui/form-data.ts';

/** Offered languages with the default first: it is the one every guest falls back to. */
export function orderedCultures(defaultCulture: string, supportedCultures: readonly string[]): string[] {
  return [defaultCulture, ...supportedCultures.filter((culture) => culture !== defaultCulture)];
}

/**
 * The texts typed for each offered language, leaving out empty ones (guests see the default language there). Only
 * offered languages are read, so a translation kept from a removed language is not sent back.
 */
export function readTranslations(
  data: FormData,
  field: string,
  cultures: readonly string[],
): Record<string, string> {
  const translations: Record<string, string> = {};
  for (const culture of cultures) {
    const text = formText(data, `${field}.${culture}`).trim();
    if (text !== '') {
      translations[culture] = text;
    }
  }

  return translations;
}

/** How a text reads in the dashboard: in the default language, or else in any language it has. */
export function displayText(
  texts: Readonly<Record<string, string>> | null | undefined,
  defaultCulture: string,
): string {
  if (texts === null || texts === undefined) {
    return '';
  }

  return texts[defaultCulture] ?? Object.values(texts)[0] ?? '';
}

/** Ids after moving some of them before or after a target, the way React Aria reports a drop. */
export function reorder(
  ids: readonly string[],
  moved: ReadonlySet<string>,
  target: string,
  position: 'before' | 'after',
): string[] {
  const remaining = ids.filter((id) => !moved.has(id));
  const index = remaining.indexOf(target);
  if (index === -1) {
    return [...ids];
  }

  const insertAt = position === 'before' ? index : index + 1;
  return [
    ...remaining.slice(0, insertAt),
    ...ids.filter((id) => moved.has(id)),
    ...remaining.slice(insertAt),
  ];
}

/**
 * Problems with a whole set of translations (such as the missing default language) are reported for the set; they
 * are shown on the default language's input, which is the one to fix.
 */
export function withTranslationErrors(
  fields: Readonly<Record<string, string>>,
  translationFields: readonly string[],
  defaultCulture: string,
): Record<string, string> {
  const errors: Record<string, string> = { ...fields };
  for (const field of translationFields) {
    const message = fields[field];
    if (message !== undefined) {
      errors[`${field}.${defaultCulture}`] = message;
    }
  }

  return errors;
}
