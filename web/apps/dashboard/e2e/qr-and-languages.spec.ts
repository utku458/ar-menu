import AxeBuilder from '@axe-core/playwright';
import type { Page } from '@playwright/test';

import { slug } from './support/fake-api.ts';
import { expect, signIn, test, toggle } from './support/test.ts';

test('QR codes encode the menu link, per language and per table', async ({ page }) => {
  await signIn(page);
  await page.getByRole('link', { name: 'QR kodları' }).click();
  const link = page.getByRole('textbox', { name: 'Menü bağlantısı' });

  await expect(link).toHaveValue(`https://armenu.test/m/${slug}`);
  await expect(
    page.getByRole('img', { name: `https://armenu.test/m/${slug} adresini açan QR kod` }),
  ).toBeVisible();

  await page.getByRole('button', { name: /Menü dili/ }).click();
  await page.getByRole('option', { name: 'English' }).click();
  await expect(link).toHaveValue(`https://armenu.test/m/${slug}?lang=en`);

  await page.getByRole('textbox', { name: 'Masa sayısı' }).fill('3');
  await page.getByRole('textbox', { name: 'Masa sayısı' }).blur();
  const cards = page.getByRole('list', { name: 'Masa kartları' }).getByRole('listitem');
  await expect(cards).toHaveCount(3);
  await expect(cards.nth(2).getByRole('img')).toHaveAccessibleName(
    `https://armenu.test/m/${slug}?lang=en&table=3 adresini açan QR kod`,
  );
});

test('languages are offered, made default and saved in one step', async ({ page, api }) => {
  await signIn(page);
  await page.getByRole('link', { name: 'Ayarlar' }).click();

  await page.getByRole('button', { name: /Dil ekle/ }).click();
  await page.getByRole('option', { name: 'Deutsch' }).click();
  await toggle(page.getByRole('radio', { name: 'Deutsch varsayılan olsun' }));
  await expect(page.getByRole('radio', { name: 'Deutsch varsayılan olsun' })).toBeChecked();
  await page.getByRole('region', { name: 'Diller' }).getByRole('button', { name: 'Kaydet' }).click();

  await expect(page.getByText('Diller kaydedildi.')).toBeVisible();
  expect(api.requestsTo('PUT', '/api/v1/manage/settings/languages')[0]?.body).toEqual({
    defaultCulture: 'de',
    supportedCultures: ['tr', 'en', 'de'],
  });
});

async function violations(page: Page) {
  const results = await new AxeBuilder({ page })
    // React Aria's off-screen announcer reads transient messages through role="img" + aria-labelledby, which axe
    // cannot resolve once the referenced element changes. It is library-owned and not part of the page.
    .exclude('[data-live-announcer]')
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
    .analyze();
  return results.violations.map((violation) => ({
    rule: violation.id,
    targets: violation.nodes.map((node) => node.target.join(' ')),
  }));
}

test('signing in, the menu editor, an item dialog and the QR page meet WCAG 2.2 AA checks', async ({
  page,
}) => {
  await page.goto(`${slug}/sign-in`);
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  expect(await violations(page)).toEqual([]);

  await signIn(page);
  expect(await violations(page)).toEqual([]);

  await page.getByRole('button', { name: 'İşlemler: Klasik Smash Burger' }).click();
  await page.getByRole('menuitem', { name: 'Düzenle' }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  expect(await violations(page)).toEqual([]);
  await page.keyboard.press('Escape');

  await page.getByRole('link', { name: 'QR kodları' }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'QR kodları' })).toBeVisible();
  expect(await violations(page)).toEqual([]);
});
