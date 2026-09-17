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

test('the owner invites a manager by e-mail, in the language of the dashboard', async ({ page, api }) => {
  await openTeam(page);
  await expect(page.getByText('Deniz Yılmaz (siz)')).toBeVisible();

  await page.getByRole('button', { name: 'Davet et' }).click();
  const dialog = page.getByRole('dialog', { name: 'Ekibe davet et' });
  await dialog.getByRole('textbox', { name: 'E-posta' }).fill('sef@armenu.test');
  await dialog.getByRole('button', { name: /Rol/ }).click();
  await page.getByRole('option', { name: 'Yönetici' }).click();
  await expect(dialog.getByText('Menüyü, fiyatları ve 3D modelleri düzenler.')).toBeVisible();
  await dialog.getByRole('button', { name: 'Daveti gönder' }).click();

  await expect(page.getByText('sef@armenu.test adresine davet gönderildi.')).toBeVisible();
  expect(api.requestsTo('POST', '/api/v1/manage/team/invitations')[0]?.body).toEqual({
    email: 'sef@armenu.test',
    role: 'Manager',
    language: 'tr',
  });
  await expect(page.getByRole('listitem').filter({ hasText: 'sef@armenu.test' })).toContainText(
    '22 Eyl 2026 tarihine kadar geçerli',
  );
  expect(await violations(page)).toEqual([]);
});

test('an e-mail that did not go out is said so, and can be sent again', async ({ page, api }) => {
  api.emailDelivers = false;
  await openTeam(page);

  await page.getByRole('button', { name: 'Davet et' }).click();
  await page.getByRole('textbox', { name: 'E-posta' }).fill('garson@armenu.test');
  await page.getByRole('button', { name: 'Daveti gönder' }).click();
  await expect(
    page.getByText('Davet kaydedildi ancak e-posta gönderilemedi.', { exact: false }),
  ).toBeVisible();

  api.emailDelivers = true;
  await page.getByRole('button', { name: 'Tekrar gönder: garson@armenu.test' }).click();
  await expect(page.getByText('garson@armenu.test adresine davet gönderildi.')).toBeVisible();

  // Inviting the same address again points at the list instead.
  await page.getByRole('button', { name: 'Davet et' }).click();
  await page.getByRole('textbox', { name: 'E-posta' }).fill('garson@armenu.test');
  await page.getByRole('button', { name: 'Daveti gönder' }).click();
  await expect(
    page.getByRole('dialog').getByText('Bu adrese zaten bekleyen bir davet var.', { exact: false }),
  ).toBeVisible();
});

test('members change role and leave the team after confirmation; invitations are withdrawn', async ({
  page,
  api,
}) => {
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

  await page.getByRole('button', { name: 'Davet et' }).click();
  await page.getByRole('textbox', { name: 'E-posta' }).fill('kasa@armenu.test');
  await page.getByRole('button', { name: 'Daveti gönder' }).click();
  await page.getByRole('button', { name: 'Daveti geri al: kasa@armenu.test' }).click();
  await page.getByRole('alertdialog').getByRole('button', { name: 'Daveti geri al' }).click();
  await expect(page.getByText('Bekleyen davet yok.')).toBeVisible();
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
