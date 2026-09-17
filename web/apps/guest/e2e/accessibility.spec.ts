import AxeBuilder from '@axe-core/playwright';
import type { Page } from '@playwright/test';

import { ids } from './support/menus.ts';
import { expect, test } from './support/test.ts';

const wcagAA = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];

async function violations(page: Page) {
  const results = await new AxeBuilder({ page }).withTags(wcagAA).analyze();
  return results.violations.map((violation) => ({
    rule: violation.id,
    impact: violation.impact,
    targets: violation.nodes.map((node) => node.target.join(' ')),
  }));
}

for (const colorScheme of ['light', 'dark'] as const) {
  test.describe(`${colorScheme} theme`, () => {
    test.use({ colorScheme });

    test('the menu meets WCAG 2.2 AA checks', async ({ page }) => {
      await page.goto('kadikoy-burger-lab');
      await expect(page.getByRole('heading', { level: 1 })).toBeVisible();

      expect(await violations(page)).toEqual([]);
    });

    test('an open dish sheet meets WCAG 2.2 AA checks', async ({ page }) => {
      await page.goto(`kadikoy-burger-lab?item=${ids.smashBurger}`);
      await expect(page.getByRole('dialog')).toBeVisible();
      await expect(page.getByRole('dialog').getByRole('figure')).toBeVisible();

      expect(await violations(page)).toEqual([]);
    });
  });
}

test('a right-to-left menu meets WCAG 2.2 AA checks', async ({ page }) => {
  await page.goto('seaside-grill');
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();

  expect(await violations(page)).toEqual([]);
});
