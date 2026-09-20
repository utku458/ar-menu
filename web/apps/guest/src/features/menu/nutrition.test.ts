import { describe, expect, it } from 'vitest';

import { macroShares, type Nutrition } from './nutrition.ts';

function dish(partial: Partial<Nutrition>): Nutrition {
  return { kcal: 0, proteinGrams: 0, carbohydrateGrams: 0, fatGrams: 0, perServing: true, ...partial };
}

describe('macroShares', () => {
  it('splits by energy, not by weight', () => {
    // Equal grams of fat and protein: fat carries 9 kcal per gram against protein's 4.
    const shares = macroShares(dish({ proteinGrams: 10, fatGrams: 10 }));

    expect(shares.find((share) => share.key === 'fat')?.energyShare).toBeCloseTo(9 / 13);
    expect(shares.find((share) => share.key === 'protein')?.energyShare).toBeCloseTo(4 / 13);
  });

  it('totals exactly one even when the declared calories disagree with the macros', () => {
    const shares = macroShares(dish({ kcal: 999, proteinGrams: 25, carbohydrateGrams: 40, fatGrams: 22 }));

    expect(shares.reduce((total, share) => total + share.energyShare, 0)).toBeCloseTo(1);
  });

  it('returns nothing when a dish declares no macronutrients', () => {
    expect(macroShares(dish({ kcal: 420 }))).toEqual([]);
  });
});
