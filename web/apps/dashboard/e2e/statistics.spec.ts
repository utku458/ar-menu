import AxeBuilder from '@axe-core/playwright';

import { expect, signIn, test } from './support/test.ts';

test('statistics show the period’s totals, a daily chart with a table, and the dishes guests open', async ({
  page,
  api,
}) => {
  api.dailyMenuViews = [12, 30, 18, 0, 44, 51, 27];
  await signIn(page);
  await page.getByRole('link', { name: 'İstatistikler' }).click();

  await expect(page.getByRole('heading', { level: 1, name: 'İstatistikler' })).toBeVisible();
  const totals = page.locator('dl');
  await expect(totals).toContainText('Menü görüntüleme182');
  await expect(totals).toContainText('AR başlatma');

  const chart = page.getByRole('list', { name: 'Günlük menü görüntüleme' });
  await expect(chart.getByRole('button')).toHaveCount(7);
  await chart.getByRole('button').nth(5).focus();
  await expect(page.getByText('14 Eyl: 51 görüntüleme')).toBeVisible();

  await page.getByText('Tablo olarak göster').click();
  await expect(page.getByRole('row', { name: /13 Eyl/ })).toContainText('44');

  const dishes = page.getByRole('table').filter({ hasText: 'AR oranı' });
  await expect(dishes.getByRole('row', { name: /Klasik Smash Burger/ })).toContainText('%30');
  await expect(dishes.getByRole('row', { name: /Limonata/ })).toContainText('%0');

  await page.getByRole('radio', { name: 'Son 30 gün' }).click();
  await expect(page).toHaveURL(/days=30/);
  expect(api.requests.some((request) => request.path === '/api/v1/manage/statistics')).toBe(true);

  const results = await new AxeBuilder({ page })
    .exclude('[data-live-announcer]')
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
    .analyze();
  expect(results.violations.map((violation) => violation.id)).toEqual([]);
});

test('a business without guests yet is told so, and staff do not see statistics', async ({ page }) => {
  await signIn(page);
  await page.getByRole('link', { name: 'İstatistikler' }).click();
  await expect(page.getByText('Bu dönemde henüz misafir hareketi yok.', { exact: false })).toBeVisible();
});

test('staff have no statistics page', async ({ page }) => {
  await signIn(page, 'staff');
  await expect(page.getByRole('link', { name: 'İstatistikler' })).toHaveCount(0);
  await page.goto('kadikoy-burger-lab/statistics');
  await expect(page.getByRole('heading', { level: 1, name: 'Menü' })).toBeVisible();
});
