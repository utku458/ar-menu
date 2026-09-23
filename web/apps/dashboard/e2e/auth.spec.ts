import { slug } from './support/fake-api.ts';
import { expect, signIn, test } from './support/test.ts';

test('an unknown visitor is asked who they are, without naming a business', async ({ page }) => {
  await page.goto('');

  await expect(page).toHaveURL(/\/sign-in/);
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Giriş yapın');
  await expect(page.getByRole('textbox', { name: 'Kullanıcı adı' })).toBeVisible();
});

test('a wrong password is explained, the right one opens the menu', async ({ page }) => {
  await page.goto(`${slug}/menu`);
  await expect(page).toHaveURL(/sign-in/);

  await page.getByRole('textbox', { name: 'Kullanıcı adı' }).fill('owner');
  await page.getByRole('textbox', { name: 'Şifre' }).fill('not-the-password');
  await page.getByRole('button', { name: 'Giriş yap' }).click();

  await expect(page.getByRole('alert')).toHaveText('E-posta veya şifre hatalı.');

  await page.getByRole('textbox', { name: 'Şifre' }).fill('correct-horse-battery');
  await page.getByRole('button', { name: 'Giriş yap' }).click();

  await expect(page).toHaveURL(new RegExp(`/${slug}/menu`));
});

test('a reload keeps the session through the refresh cookie, without storing the token', async ({
  page,
  api,
}) => {
  await signIn(page);

  await page.reload();

  await expect(page.getByRole('heading', { level: 1, name: 'Menü' })).toBeVisible();
  const refresh = api.requestsTo('POST', `/api/v1/tenants/${slug}/auth/refresh`).at(-1);
  expect(refresh?.headers.cookie).toMatch(/armenu_refresh=refresh-owner-/);
  const stored = await page.evaluate(() =>
    [localStorage, sessionStorage].flatMap((storage) =>
      Object.keys(storage).map((key) => storage.getItem(key)),
    ),
  );
  expect(stored.join('\n')).not.toContain('access-');
});

test('signing out ends the session for good', async ({ page }) => {
  await signIn(page);

  await page.getByRole('button', { name: /Deniz Yılmaz/ }).click();
  await page.getByRole('menuitem', { name: 'Çıkış yap' }).click();

  await expect(page).toHaveURL(/sign-in/);
  await page.goto(`${slug}/menu`);
  await expect(page).toHaveURL(/sign-in/);
});
