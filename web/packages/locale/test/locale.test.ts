import { describe, expect, test } from 'vitest';

import { formatPrice, languageName, textDirection } from '../src/index.ts';

describe('textDirection', () => {
  test.each([
    ['ar', 'rtl'],
    ['fa-ir', 'rtl'],
    ['tr', 'ltr'],
    ['zh-hant', 'ltr'],
  ])('%s is written %s', (culture, direction) => {
    expect(textDirection(culture)).toBe(direction);
  });
});

describe('languageName', () => {
  test.each([
    ['de', 'Deutsch'],
    ['tr', 'Türkçe'],
    ['ru', 'Русский'],
  ])('names %s in its own language', (culture, name) => {
    expect(languageName(culture)).toBe(name);
  });

  test('falls back to the code for tags Intl cannot handle', () => {
    expect(languageName('not a tag')).toBe('not a tag');
  });
});

describe('formatPrice', () => {
  // Intl output contains non-breaking spaces in some locales; compare with regular spaces.
  const format = (amount: number, currency: string, culture: string) =>
    formatPrice(amount, currency, culture).replace(/\s/g, ' ');

  test('uses the local currency symbol and hides decimals of whole prices', () => {
    expect(format(385, 'TRY', 'tr')).toBe('₺385');
    expect(format(385, 'TRY', 'en')).toBe('₺385');
  });

  test('keeps both decimals when a price has cents', () => {
    expect(format(12.5, 'EUR', 'de')).toBe('12,50 €');
  });
});
