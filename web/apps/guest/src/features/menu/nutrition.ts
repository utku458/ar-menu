/**
 * Nutrition for a dish.
 *
 * **This is not in the API yet.** `PublicMenuItemResponse` carries price, allergens and dietary labels and nothing
 * else, so nothing on the wire fills this in today. It is declared here, in the shape the guest app wants to read,
 * so the interface can be built and reviewed now and the backend has a target to hit: add `nutrition` to
 * `PublicMenuItemResponse`, regenerate the client, and pass it through — no component below changes.
 *
 * Units are grams and kilocalories, matching EU Regulation 1169/2011, which also fixes the reference quantity at
 * 100 g. `perServing` records which of the two a restaurant declared, because showing a per-100 g figure as if it
 * were the plate in front of someone is the kind of error that matters to a diabetic guest.
 */
export interface Nutrition {
  readonly kcal: number;
  readonly proteinGrams: number;
  readonly carbohydrateGrams: number;
  readonly fatGrams: number;
  readonly perServing: boolean;
}

export type MacroKey = 'protein' | 'carbohydrate' | 'fat';

export interface MacroShare {
  readonly key: MacroKey;
  readonly grams: number;
  /** Share of energy, 0–1 — not share of weight. */
  readonly energyShare: number;
}

/** Atwater factors: the kcal each gram of a macronutrient contributes. */
const KCAL_PER_GRAM: Readonly<Record<MacroKey, number>> = {
  protein: 4,
  carbohydrate: 4,
  fat: 9,
};

/**
 * Splits a dish into the share of its energy each macronutrient provides.
 *
 * Deliberately *not* the share of grams, which is the mistake every macro ring on the web makes: a gram of fat
 * carries more than twice the energy of a gram of protein, so a weight-proportional chart shows a buttery dish as
 * mostly carbohydrate. The denominator is the energy computed from the macros themselves rather than the declared
 * `kcal`, so the bars always total exactly 100 % even when a restaurant's own figures do not quite add up.
 *
 * Returns an empty list when a dish declares no macronutrients at all, so callers can omit the whole section rather
 * than draw three zeroes.
 */
export function macroShares({ proteinGrams, carbohydrateGrams, fatGrams }: Nutrition): readonly MacroShare[] {
  const grams: Readonly<Record<MacroKey, number>> = {
    protein: proteinGrams,
    carbohydrate: carbohydrateGrams,
    fat: fatGrams,
  };

  const totalKcal = (Object.keys(grams) as MacroKey[]).reduce(
    (total, key) => total + grams[key] * KCAL_PER_GRAM[key],
    0,
  );
  if (totalKcal <= 0) {
    return [];
  }

  return (Object.keys(grams) as MacroKey[]).map((key) => ({
    key,
    grams: grams[key],
    energyShare: (grams[key] * KCAL_PER_GRAM[key]) / totalKcal,
  }));
}
