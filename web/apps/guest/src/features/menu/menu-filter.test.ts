import type { PublicMenuCategoryResponse, PublicMenuItemResponse } from '@armenu/api-client';
import { describe, expect, test } from 'vitest';

import { filterMenu, isFiltering, noFilter, searchable } from './menu-filter.ts';

function dish(
  name: string,
  allergens: string[] | null,
  dietaryLabels: string[] = [],
  description: string | null = null,
): PublicMenuItemResponse {
  return {
    id: name,
    name,
    description,
    price: 1,
    isAvailable: true,
    arModel: null,
    allergens,
    dietaryLabels,
  };
}

const menu: PublicMenuCategoryResponse[] = [
  {
    id: 'mains',
    name: 'Ana yemekler',
    description: null,
    items: [
      dish('Adana Kebap', ['gluten']),
      dish('Tavuk Şiş', [], [], 'Közlenmiş biber ile'),
      dish('İskender', ['gluten', 'milk']),
    ],
  },
  {
    id: 'salads',
    name: 'Salatalar',
    description: null,
    items: [
      dish('Çoban Salata', [], ['vegetarian', 'vegan', 'glutenFree']),
      dish('Günün salatası', null, ['vegetarian']),
    ],
  },
];

const names = (categories: readonly PublicMenuCategoryResponse[]) =>
  categories.flatMap((category) => category.items.map((item) => item.name));

describe('searchable', () => {
  test.each([
    ['Köfte', 'tr', 'kofte'],
    ['ŞİŞ', 'tr', 'sis'],
    ['Kısır', 'tr', 'kisir'],
    ['Crème Brûlée', 'en', 'creme brulee'],
    ['  two   spaces ', 'en', 'two spaces'],
  ])('%s in %s reads as %s', (text, culture, expected) => {
    expect(searchable(text, culture)).toBe(expected);
  });
});

describe('filterMenu', () => {
  test('without a filter the menu is left exactly as it is', () => {
    const result = filterMenu(menu, noFilter, 'tr');

    expect(result.categories).toBe(menu);
    expect(result.matches).toBe(5);
    expect(isFiltering(noFilter)).toBe(false);
  });

  test('every word must appear in the name or the description, however it is typed', () => {
    expect(names(filterMenu(menu, { ...noFilter, query: 'sis' }, 'tr').categories)).toEqual(['Tavuk Şiş']);
    expect(names(filterMenu(menu, { ...noFilter, query: 'biber tavuk' }, 'tr').categories)).toEqual([
      'Tavuk Şiş',
    ]);
    expect(names(filterMenu(menu, { ...noFilter, query: 'iskender' }, 'tr').categories)).toEqual([
      'İskender',
    ]);
  });

  test('a diet keeps only dishes labelled with it, and sections left empty disappear', () => {
    const result = filterMenu(menu, { ...noFilter, diets: ['vegan'] }, 'tr');

    expect(names(result.categories)).toEqual(['Çoban Salata']);
    expect(result.categories.map((category) => category.id)).toEqual(['salads']);
  });

  test('avoiding an allergen never shows a dish whose allergens nobody declared, and counts it instead', () => {
    const result = filterMenu(menu, { ...noFilter, avoid: ['gluten'] }, 'tr');

    expect(names(result.categories)).toEqual(['Tavuk Şiş', 'Çoban Salata']);
    expect(result.undeclared).toBe(1);
  });

  test('an undeclared dish that the search would not have shown anyway is not counted', () => {
    const result = filterMenu(menu, { query: 'kebap', diets: [], avoid: ['milk'] }, 'tr');

    expect(names(result.categories)).toEqual(['Adana Kebap']);
    expect(result.undeclared).toBe(0);
  });
});
