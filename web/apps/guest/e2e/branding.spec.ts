import type { Page } from '@playwright/test';

import { expect, test } from './support/test.ts';

/** The colours are read back from the document element, which is where the menu sets them. */
function brandTokens(page: Page) {
  return page.evaluate(() => {
    const style = getComputedStyle(document.documentElement);
    return {
      brand: style.getPropertyValue('--color-brand').trim(),
      ink: style.getPropertyValue('--color-brand-ink').trim(),
    };
  });
}

test('a business with a logo and a colour shows both on its menu', async ({ page }) => {
  await page.goto('kadikoy-burger-lab');
  await expect(page.getByRole('heading', { level: 1, name: 'Kadıköy Burger Lab' })).toBeVisible();

  // The logo is decorative: the business's name is right next to it, so a screen reader must not read it twice.
  const logo = page.locator('header img');
  await expect(logo).toHaveAttribute('alt', '');
  await expect(logo).toHaveJSProperty('complete', true);

  expect(await brandTokens(page)).toEqual({ brand: '#1f6f5c', ink: '#ffffff' });
});

test('a business without a colour keeps the platform’s own', async ({ page }) => {
  await page.goto('seaside-grill');
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();

  await expect(page.locator('header img')).toHaveCount(0);
  const { brand } = await brandTokens(page);
  expect(brand).not.toBe('#1f6f5c');
  expect(brand).not.toBe('');
});
