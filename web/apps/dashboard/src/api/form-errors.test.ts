import { ApiError } from '@armenu/api-client';
import { describe, expect, test } from 'vitest';

import { formErrorsFrom, toFieldName } from './form-errors.ts';

const text = {
  describe: (code: string | undefined, fallback?: string) =>
    code === 'money.amount_negative' ? 'Fiyat negatif olamaz.' : (fallback ?? 'Beklenmeyen hata.'),
  networkError: 'Sunucuya ulaşılamadı.',
};

describe('formErrorsFrom', () => {
  test('places translated validation messages next to their fields', () => {
    const error = new ApiError(400, {
      status: 400,
      code: 'validation.failed',
      errors: { price: ['Amount cannot be negative.'], 'name[tr]': ['Too long.'] },
      errorCodes: { price: ['money.amount_negative'], 'name[tr]': ['menu_item.name_too_long'] },
    } as never);

    expect(formErrorsFrom(error, text)).toEqual({
      fields: { price: 'Fiyat negatif olamaz.', 'name.tr': 'Too long.' },
      form: undefined,
    });
  });

  test('reports problems without field details for the whole form', () => {
    const error = new ApiError(409, { status: 409, code: 'tenant.slug_taken', detail: 'The slug is taken.' });

    expect(formErrorsFrom(error, text)).toEqual({ fields: {}, form: 'The slug is taken.' });
  });

  test('explains network failures as such', () => {
    expect(formErrorsFrom(new TypeError('Failed to fetch'), text).form).toBe('Sunucuya ulaşılamadı.');
  });
});

describe('toFieldName', () => {
  test.each([
    ['price', 'price'],
    ['name[tr]', 'name.tr'],
    ['supportedCultures[2]', 'supportedCultures.2'],
  ])('%s → %s', (path, field) => {
    expect(toFieldName(path)).toBe(field);
  });
});
