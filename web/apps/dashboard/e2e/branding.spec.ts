import { fileURLToPath } from 'node:url';

import AxeBuilder from '@axe-core/playwright';
import type { Page } from '@playwright/test';

import { expect, signIn, test } from './support/test.ts';

const logo = fileURLToPath(new URL('../../../../assets/demo/posters/sea-bass.webp', import.meta.url));

test('the name, the logo and the colour are saved together', async ({ page, api }) => {
  await signIn(page);
  await page.getByRole('link', { name: 'Ayarlar' }).click();
  const card = page.getByRole('region', { name: 'Görünüm' });

  await card.getByRole('textbox', { name: 'İşletme adı' }).fill('Kadıköy Burger Lab & Kahve');
  await card.locator('input[type=file]').setInputFiles(logo);
  await expect(card.getByRole('button', { name: 'Logoyu kaldır' })).toBeVisible();
  expect(await violations(page)).toEqual([]);

  await card.getByRole('button', { name: 'Kaydet' }).click();

  await expect(page.getByText('Görünüm kaydedildi.')).toBeVisible();
  const saved = api.requestsTo('PUT', '/api/v1/manage/settings/branding')[0]?.body as {
    name: string;
    logoPath: string;
    accentColor: string | null;
  };
  expect(saved.name).toBe('Kadıköy Burger Lab & Kahve');
  expect(saved.logoPath).toMatch(/\.webp$/);
  // Nothing was picked in the colour field, so the menu keeps the platform's own colour.
  expect(saved.accentColor).toBeNull();
});

test('a file the API would refuse never leaves the browser', async ({ page, api }) => {
  await signIn(page);
  await page.getByRole('link', { name: 'Ayarlar' }).click();
  const card = page.getByRole('region', { name: 'Görünüm' });

  await card.locator('input[type=file]').setInputFiles({
    name: 'logo.svg',
    mimeType: 'image/svg+xml',
    buffer: Buffer.from('<svg xmlns="http://www.w3.org/2000/svg" />'),
  });

  await expect(card.getByRole('alert')).toHaveText('Bu alan bu dosya türünü kabul etmiyor.');
  expect(api.uploads).toEqual([]);
});

async function violations(page: Page) {
  // React Aria's live-region shim is excluded: its markup belongs to the library, not to the page.
  const { violations: found } = await new AxeBuilder({ page })
    .exclude('[data-live-announcer]')
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
    .analyze();
  return found.map((violation) => violation.id);
}
