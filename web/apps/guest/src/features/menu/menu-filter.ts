import type { PublicMenuCategoryResponse, PublicMenuItemResponse } from '@armenu/api-client';

export const dietaryLabelCodes = ['vegetarian', 'vegan', 'glutenFree'] as const;

/** The allergens guests most often avoid first; the rest follow in the API's order. */
export const allergenCodes = [
  'gluten',
  'milk',
  'nuts',
  'peanuts',
  'eggs',
  'fish',
  'crustaceans',
  'molluscs',
  'soybeans',
  'sesame',
  'celery',
  'mustard',
  'sulphites',
  'lupin',
] as const;

export interface MenuFilter {
  readonly query: string;
  /** Diets a dish must suit, all of them. */
  readonly diets: readonly string[];
  /** Allergens a dish must be declared not to contain. */
  readonly avoid: readonly string[];
}

export const noFilter: MenuFilter = { query: '', diets: [], avoid: [] };

export interface FilteredMenu {
  readonly categories: readonly PublicMenuCategoryResponse[];
  readonly matches: number;
  /**
   * Dishes left out only because the business never said what allergens they contain. Guests are told, so an empty
   * result does not read as "nothing here is safe" and a full one not as "everything else was checked".
   */
  readonly undeclared: number;
}

export function isFiltering(filter: MenuFilter): boolean {
  return filter.query.trim() !== '' || filter.diets.length > 0 || filter.avoid.length > 0;
}

/**
 * Text as it is compared in a search: case folded in the menu's language, accents removed, and the Turkish dotless ı
 * read as i, so a tourist typing "kofte" or "sis" finds "Köfte" and "Şiş".
 */
export function searchable(text: string, culture: string): string {
  return text
    .toLocaleLowerCase(culture)
    .normalize('NFD')
    .replace(/\p{M}/gu, '')
    .replaceAll('ı', 'i')
    .replace(/\s+/g, ' ')
    .trim();
}

/**
 * The menu with only the dishes that match, and without the sections left empty. Everything runs on the menu already in
 * the browser: what a guest searches for, or is allergic to, never leaves the phone.
 */
export function filterMenu(
  categories: readonly PublicMenuCategoryResponse[],
  filter: MenuFilter,
  culture: string,
): FilteredMenu {
  if (!isFiltering(filter)) {
    return {
      categories,
      matches: categories.reduce((count, category) => count + category.items.length, 0),
      undeclared: 0,
    };
  }

  const words = searchable(filter.query, culture).split(' ').filter(Boolean);
  let matches = 0;
  let undeclared = 0;

  const filtered = categories.flatMap((category) => {
    const items = category.items.filter((item) => {
      if (
        !matchesWords(item, words, culture) ||
        !filter.diets.every((diet) => item.dietaryLabels.includes(diet))
      ) {
        return false;
      }

      if (filter.avoid.length > 0 && item.allergens === null) {
        undeclared++;
        return false;
      }

      return !filter.avoid.some((allergen) => item.allergens?.includes(allergen) === true);
    });

    matches += items.length;
    return items.length === 0 ? [] : [{ ...category, items }];
  });

  return { categories: filtered, matches, undeclared };
}

function matchesWords(item: PublicMenuItemResponse, words: readonly string[], culture: string): boolean {
  if (words.length === 0) {
    return true;
  }

  const text = searchable(`${item.name} ${item.description ?? ''}`, culture);
  return words.every((word) => text.includes(word));
}

export function toggled(values: readonly string[], value: string): string[] {
  return values.includes(value) ? values.filter((candidate) => candidate !== value) : [...values, value];
}
