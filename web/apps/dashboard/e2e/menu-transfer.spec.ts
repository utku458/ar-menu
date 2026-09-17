import { readFile } from 'node:fs/promises';

import AxeBuilder from '@axe-core/playwright';

import { ids, slug } from './support/fake-api.ts';
import { expect, signIn, test } from './support/test.ts';

test('the menu downloads as a spreadsheet named after the business', async ({ page }) => {
  await signIn(page);

  const download = page.waitForEvent('download');
  await page.getByRole('button', { name: 'Dışa aktar' }).click();
  const file = await download;

  expect(file.suggestedFilename()).toBe(`${slug}-menu-2026-09-15.csv`);
  expect(await readFile(await file.path(), 'utf8')).toContain(`${ids.lemonade},İçecekler,Limonata`);
});

test('an import shows its problems first, then what it changes, and applies only a clean file', async ({
  page,
  api,
}) => {
  await signIn(page);
  await page.getByRole('button', { name: 'İçe aktar' }).click();
  const dialog = page.getByRole('dialog', { name: 'Menüyü tablodan içe aktar' });

  const chooser = page.waitForEvent('filechooser');
  await dialog.getByRole('button', { name: 'Dosya seç' }).click();
  await (
    await chooser
  ).setFiles({
    name: 'menu.csv',
    mimeType: 'text/csv',
    buffer: Buffer.from(`id;name:tr;price;available\n${ids.lemonade};Limonata;135;belki\n`),
  });

  await expect(dialog.getByRole('alert')).toContainText('Dosyada 1 sorun var.');
  await expect(dialog.getByRole('row', { name: /2 available evet veya hayır yazın/ })).toBeVisible();
  await expect(dialog.getByRole('button', { name: 'Değişiklikleri uygula' })).toBeDisabled();
  expect(api.requestsTo('POST', '/api/v1/manage/menu/import')).toHaveLength(1);

  const results = await new AxeBuilder({ page })
    .exclude('[data-live-announcer]')
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
    .analyze();
  expect(results.violations.map((violation) => violation.id)).toEqual([]);

  const again = page.waitForEvent('filechooser');
  await dialog.getByRole('button', { name: 'Dosya seç' }).click();
  await (
    await again
  ).setFiles({
    name: 'menu.csv',
    mimeType: 'text/csv',
    buffer: Buffer.from(`id;name:tr;price\n${ids.lemonade};Limonata;135\n`),
  });

  await expect(
    dialog.getByText('0 ürün eklenecek, 1 ürün güncellenecek, 0 ürün aynı kalacak.'),
  ).toBeVisible();
  await expect(dialog.getByRole('listitem').filter({ hasText: 'Limonata' })).toContainText('Fiyat');
  await dialog.getByRole('button', { name: 'Değişiklikleri uygula' }).click();

  await expect(page.getByText('0 ürün eklendi, 1 ürün güncellendi.')).toBeVisible();
  const imports = api.requestsTo('POST', '/api/v1/manage/menu/import');
  expect(imports.at(-1)?.headers['content-type']).toContain('text/csv');
  expect(imports.at(-1)?.body).toBe(`id;name:tr;price\n${ids.lemonade};Limonata;135\n`);
});
