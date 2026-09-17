import { describe, expect, test } from 'vitest';

import { displayText, orderedCultures, readTranslations, reorder } from './menu-editing.ts';

describe('orderedCultures', () => {
  test('puts the default language first', () => {
    expect(orderedCultures('en', ['tr', 'en', 'de'])).toEqual(['en', 'tr', 'de']);
  });
});

describe('readTranslations', () => {
  test('keeps filled, offered languages only', () => {
    const data = new FormData();
    data.set('name.tr', '  Humus ');
    data.set('name.en', '   ');
    data.set('name.fr', 'Houmous');

    expect(readTranslations(data, 'name', ['tr', 'en'])).toEqual({ tr: 'Humus' });
  });
});

describe('displayText', () => {
  test('prefers the default language and falls back to any translation', () => {
    expect(displayText({ tr: 'Humus', en: 'Hummus' }, 'en')).toBe('Hummus');
    expect(displayText({ tr: 'Humus' }, 'en')).toBe('Humus');
    expect(displayText(null, 'en')).toBe('');
  });
});

describe('reorder', () => {
  const ids = ['a', 'b', 'c', 'd'];

  test.each([
    [['d'], 'a', 'before', ['d', 'a', 'b', 'c']],
    [['a'], 'c', 'after', ['b', 'c', 'a', 'd']],
    [['b', 'c'], 'd', 'after', ['a', 'd', 'b', 'c']],
    [['a'], 'missing', 'before', ['a', 'b', 'c', 'd']],
  ] as const)('moves %j %s %s', (moved, target, position, expected) => {
    expect(reorder(ids, new Set(moved), target, position)).toEqual(expected);
  });
});
