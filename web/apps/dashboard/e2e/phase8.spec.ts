import AxeBuilder from '@axe-core/playwright';
import type { Page } from '@playwright/test';

import { ids, password, slug } from './support/fake-api.ts';
import { expect, signIn, test } from './support/test.ts';

async function violations(page: Page) {
  const results = await new AxeBuilder({ page })
    .exclude('[data-live-announcer]')
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
    .analyze();
  return results.violations.map((violation) => violation.id);
}

test('the owner hands the business over after confirming the password, and stays as a manager', async ({
  page,
  api,
}) => {
  await signIn(page);
  await page.getByRole('link', { name: 'Ekip' }).click();
  await page.getByRole('button', { name: 'İşlemler: Ece Kaya' }).click();
  await page.getByRole('menuitem', { name: 'Sahipliği devret' }).click();

  const dialog = page.getByRole('alertdialog', { name: 'İşletme Ece Kaya kişisine devredilsin mi?' });
  await dialog.getByLabel('Onaylamak için şifrenizi girin').fill('wrong-password');
  await dialog.getByRole('button', { name: 'Sahipliği devret' }).click();
  await expect(dialog.getByText('Şifre hatalı.')).toBeVisible();
  expect(await violations(page)).toEqual([]);

  await dialog.getByLabel('Onaylamak için şifrenizi girin').fill(password);
  await dialog.getByRole('button', { name: 'Sahipliği devret' }).click();

  await expect(page.getByText('İşletmenin sahibi artık Ece Kaya.')).toBeVisible();
  await expect(page.getByRole('heading', { level: 1, name: 'Menü' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Ekip' })).toHaveCount(0);
  expect(
    api.requestsTo('POST', `/api/v1/manage/team/members/${ids.staffMembership}/ownership`).at(-1)?.body,
  ).toEqual({ password });
});

test('an owner with a team is told to hand over first; a member deletes their account', async ({
  page,
  api,
}) => {
  await signIn(page);
  await page.goto(`${slug}/account`);
  await expect(page.getByRole('heading', { level: 1, name: 'Hesabım' })).toBeVisible();
  await expect(page.getByText('Önce devretmeniz gereken işletmeler')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Hesabımı sil' })).toBeDisabled();
  expect(await violations(page)).toEqual([]);

  await page.context().clearCookies();
  await page.goto('/');
  api.team.members.splice(1);
  api.roles.owner = 'Manager';
  await signIn(page);
  await page.getByRole('button', { name: /Deniz Yılmaz/ }).click();
  await page.getByRole('menuitem', { name: 'Hesabım' }).click();
  await expect(page.getByText('Ekibinden ayrılacağınız işletmeler')).toBeVisible();

  await page.getByRole('button', { name: 'Hesabımı sil' }).click();
  const dialog = page.getByRole('alertdialog', { name: 'Hesabınız kalıcı olarak silinsin mi?' });
  await dialog.getByLabel('Onaylamak için şifrenizi girin').fill(password);
  await dialog.getByRole('button', { name: 'Hesabımı sil' }).click();

  await expect(page.getByText('Hesabınız silindi.', { exact: false })).toBeVisible();
  await expect(page).toHaveURL(/choose=true/);
  expect(api.deleted.has('owner')).toBe(true);
});

test('history tells who changed what, in the business’s days, page by page', async ({ page }) => {
  await signIn(page);
  await page.getByRole('link', { name: 'Geçmiş' }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'Geçmiş' })).toBeVisible();

  const latest = page
    .getByRole('listitem')
    .filter({ hasText: 'Deniz Yılmaz “Klasik Smash Burger” ürününü düzenledi' });
  await expect(latest).toHaveCount(1);
  await expect(latest).toContainText('Deniz Yılmaz');
  await expect(latest).toContainText('Fiyat');
  await expect(latest).toContainText('₺385,00');
  await expect(page.getByText('“Limonata” ürününü tükendi olarak işaretledi').first()).toBeVisible();
  await expect(page.getByRole('listitem').filter({ hasText: 'Sistem' }).first()).toContainText('Var');
  await expect(page.getByRole('heading', { level: 2, name: '15 Eylül 2026 Salı' })).toBeVisible();
  expect(await violations(page)).toEqual([]);

  await expect(page.getByRole('listitem').filter({ hasText: 'ürününü' })).toHaveCount(50);
  await page.getByRole('button', { name: 'Daha eskileri göster' }).click();
  await expect(page.getByRole('listitem').filter({ hasText: 'ürününü' })).toHaveCount(52);
  await expect(page.getByRole('button', { name: 'Daha eskileri göster' })).toHaveCount(0);
});

test('the time zone is picked by typing and saved', async ({ page, api }) => {
  await signIn(page);
  await page.getByRole('link', { name: 'Ayarlar' }).click();
  const field = page.getByRole('combobox', { name: 'Saat dilimi' });
  await expect(field).toHaveValue('GMT+3 · Europe/Istanbul');

  await field.fill('');
  await field.pressSequentially('Berlin');
  await expect(field).toHaveValue('Berlin');
  await page.getByRole('option', { name: /Europe\/Berlin/ }).click();
  await page.getByRole('region', { name: 'Saat dilimi' }).getByRole('button', { name: 'Kaydet' }).click();

  await expect(page.getByText('Saat dilimi kaydedildi.')).toBeVisible();
  expect(api.requestsTo('PUT', '/api/v1/manage/settings/time-zone')[0]?.body).toEqual({
    timeZone: 'Europe/Berlin',
  });
  expect(await violations(page)).toEqual([]);
});
