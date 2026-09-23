import AxeBuilder from '@axe-core/playwright';
import type { Page } from '@playwright/test';

import { ids, invitationToken, slug } from './support/fake-api.ts';
import { expect, signIn, test } from './support/test.ts';

async function openTeam(page: Page) {
  await signIn(page);
  await page.getByRole('link', { name: 'Ekip' }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'Ekip' })).toBeVisible();
}

async function violations(page: Page) {
  const results = await new AxeBuilder({ page })
    .exclude('[data-live-announcer]')
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
    .analyze();
  return results.violations.map((violation) => violation.id);
}

test('the owner opens an account for a manager, with a user name and a password', async ({ page, api }) => {
  await openTeam(page);
  await expect(page.getByText('Deniz Yılmaz (siz)')).toBeVisible();

  await page.getByRole('button', { name: 'Kullanıcı ekle' }).click();
  const dialog = page.getByRole('dialog', { name: 'Kullanıcı ekle' });
  await dialog.getByRole('textbox', { name: 'Adınız soyadınız' }).fill('Mert Şef');
  await dialog.getByRole('textbox', { name: 'Kullanıcı adı' }).fill('mert');
  await dialog.getByRole('textbox', { name: 'Şifre' }).fill('mutfak-sifresi-2026');
  await dialog.getByRole('button', { name: /Rol/ }).click();
  await page.getByRole('option', { name: 'Yönetici' }).click();
  await expect(dialog.getByText('Menüyü, fiyatları ve 3D modelleri düzenler.')).toBeVisible();
  await dialog.getByRole('button', { name: 'Kullanıcı ekle' }).click();

  await expect(page.getByText('Kullanıcı eklendi: mert')).toBeVisible();
  expect(api.requestsTo('POST', '/api/v1/manage/team/members')[0]?.body).toEqual({
    fullName: 'Mert Şef',
    userName: 'mert',
    password: 'mutfak-sifresi-2026',
    role: 'Manager',
  });

  // The list shows the name the person signs in with, not the placeholder address the account carries.
  const added = page.getByRole('listitem').filter({ hasText: 'Mert Şef' });
  await expect(added).toContainText('mert');
  await expect(added).not.toContainText('@users.armenu.invalid');
  expect(await violations(page)).toEqual([]);
});

test('a user name that is taken is refused next to the field', async ({ page }) => {
  await openTeam(page);

  await page.getByRole('button', { name: 'Kullanıcı ekle' }).click();
  const dialog = page.getByRole('dialog', { name: 'Kullanıcı ekle' });
  await dialog.getByRole('textbox', { name: 'Adınız soyadınız' }).fill('İkinci Kasa');
  await dialog.getByRole('textbox', { name: 'Kullanıcı adı' }).fill('staff');
  await dialog.getByRole('textbox', { name: 'Şifre' }).fill('kasa-sifresi-2026');
  await dialog.getByRole('button', { name: 'Kullanıcı ekle' }).click();

  await expect(dialog.getByText('Bu kullanıcı adı zaten kullanılıyor.', { exact: false })).toBeVisible();
});

test('the owner gives a member who lost their password a new one', async ({ page, api }) => {
  await openTeam(page);

  await page.getByRole('button', { name: 'İşlemler: Ece Kaya' }).click();
  await page.getByRole('menuitem', { name: 'Şifre sıfırla' }).click();

  const dialog = page.getByRole('dialog', { name: 'Şifre sıfırla' });
  await expect(dialog.getByText('Ece Kaya (staff)', { exact: false })).toBeVisible();
  await dialog.getByRole('textbox', { name: 'Yeni şifre' }).fill('yeni-kasa-sifresi-2026');
  await dialog.getByRole('button', { name: 'Şifreyi değiştir' }).click();

  await expect(page.getByText('Ece Kaya için yeni şifre belirlendi')).toBeVisible();
  expect(
    api.requestsTo('PUT', `/api/v1/manage/team/members/${ids.staffMembership}/password`)[0]?.body,
  ).toEqual({ password: 'yeni-kasa-sifresi-2026' });
});

test('members change role and leave the team after confirmation', async ({ page, api }) => {
  await openTeam(page);

  await page.getByRole('button', { name: 'İşlemler: Ece Kaya' }).click();
  await page.getByRole('menuitem', { name: 'Yönetici yap' }).click();
  await expect(page.getByText('Ece Kaya artık yönetici.', { exact: false })).toBeVisible();
  expect(api.requestsTo('PUT', `/api/v1/manage/team/members/${ids.staffMembership}/role`)[0]?.body).toEqual({
    role: 'Manager',
  });

  await page.getByRole('button', { name: 'İşlemler: Ece Kaya' }).click();
  await page.getByRole('menuitem', { name: 'Ekipten çıkar' }).click();
  const confirmation = page.getByRole('alertdialog', { name: 'Ece Kaya ekipten çıkarılsın mı?' });
  await confirmation.getByRole('button', { name: 'Ekipten çıkar' }).click();
  await expect(page.getByText('Ece Kaya ekipten çıkarıldı.')).toBeVisible();
  await expect(page.getByText(`staff@${slug}.test`)).toHaveCount(0);

  // The owner has no actions: their role and place are not up for change.
  await expect(page.getByRole('button', { name: 'İşlemler: Deniz Yılmaz' })).toHaveCount(0);
});

test('staff see no team page, not even by its address', async ({ page }) => {
  await signIn(page, 'staff');
  await expect(page.getByRole('link', { name: 'Ekip' })).toHaveCount(0);

  await page.goto(`${slug}/team`);
  await expect(page.getByRole('heading', { level: 1, name: 'Menü' })).toBeVisible();
});

test('an invitation link creates the account, signs in and leaves no secret in the address bar', async ({
  page,
  api,
}) => {
  await page.goto(`${slug}/join#${invitationToken}`);

  await expect(
    page.getByRole('heading', { level: 1, name: 'Kadıköy Burger Lab ekibine katılın' }),
  ).toBeVisible();
  await expect(page.getByText(`yeni@${slug}.test adresine personel olarak davet edildiniz.`)).toBeVisible();
  expect(new URL(page.url()).hash).toBe('');
  expect(await violations(page)).toEqual([]);

  await page.getByRole('textbox', { name: 'Adınız soyadınız' }).fill('Can Demir');
  await page.getByRole('textbox', { name: 'Şifre' }).fill('a-long-enough-password');
  await page.getByRole('button', { name: 'Ekibe katıl' }).click();

  await expect(page.getByRole('heading', { level: 1, name: 'Menü' })).toBeVisible();
  expect(api.requestsTo('POST', `/api/v1/tenants/${slug}/invitations/accept`)[0]?.body).toEqual({
    token: invitationToken,
    fullName: 'Can Demir',
    password: 'a-long-enough-password',
  });
});

test('a link that does not work explains what to do', async ({ page }) => {
  await page.goto(`${slug}/join#not-a-valid-token`);

  await expect(page.getByRole('heading', { level: 1, name: 'Bu davet kullanılamıyor' })).toBeVisible();
  await expect(page.getByRole('alert')).toHaveText(
    'Bu davet bağlantısı geçerli değil. İşletmeden yeni bir davet isteyin.',
  );

  await page.goto(`${slug}/join`);
  await expect(page.getByRole('alert')).toHaveText('Bu sayfayı davet e-postanızdaki bağlantıyla açın.');
});
