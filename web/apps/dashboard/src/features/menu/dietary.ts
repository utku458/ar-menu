/** The fourteen allergens of EU Regulation 1169/2011, in the order the API returns them. */
export const allergenCodes = [
  'gluten',
  'crustaceans',
  'eggs',
  'fish',
  'peanuts',
  'soybeans',
  'milk',
  'nuts',
  'celery',
  'mustard',
  'sesame',
  'sulphites',
  'lupin',
  'molluscs',
] as const;

export const dietaryLabelCodes = ['vegetarian', 'vegan', 'glutenFree'] as const;

/**
 * Reads the dietary fields of a submitted item form. Allergens that were not declared are sent as null, which the API
 * keeps apart from an empty list ("contains none").
 */
export function readDietary(data: FormData): { allergens: string[] | null; dietaryLabels: string[] } {
  return {
    allergens: data.get('allergensDeclared') === 'on' ? data.getAll('allergens').map(String) : null,
    dietaryLabels: data.getAll('dietaryLabels').map(String),
  };
}

/** The API reports an unknown code on its index (`allergens[3]`); the form shows it on the group. */
export function withDietaryErrors(fields: Readonly<Record<string, string>>): Record<string, string> {
  const errors: Record<string, string> = { ...fields };
  for (const [field, message] of Object.entries(fields)) {
    const group = /^(allergens|dietaryLabels)\[\d+\]$/.exec(field)?.[1];
    if (group !== undefined) {
      errors[group] = message;
    }
  }

  return errors;
}
