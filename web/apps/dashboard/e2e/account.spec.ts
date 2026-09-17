import { slug } from './support/fake-api.ts';
import { expect, signIn, test } from './support/test.ts';

test('a forgotten password is reset through the e-mailed link, without revealing accounts', async ({
  page,
  api,
}) => {
  await page.goto(`${slug}/sign-in`);
  await page.getByRole('link', { name: 'Şifremi unuttum' }).click();

  await page.getByRole('textbox', { name: 'E-posta' }).fill('someone@example.com');
  await page.getByRole('button', { name: 'Bağlantıyı gönder' }).click();
  await expect(page.getByRole('status')).toContainText('someone@example.com adresine ait bir hesap varsa');
  expect(api.requestsTo('POST', '/api/v1/auth/password-reset')[0]?.body).toEqual({
    email: 'someone@example.com',
    language: 'tr',
  });
  await expect(page.getByRole('link', { name: 'Girişe dön' })).toHaveAttribute('href', `/${slug}/sign-in`);

  await page.goto(`reset-password#${api.accountLinkToken}`);
  await expect(page.getByRole('heading', { level: 1, name: 'Yeni şifre belirleyin' })).toBeVisible();
  expect(new URL(page.url()).hash).toBe('');
  await page.getByRole('textbox', { name: 'Yeni şifre' }).fill('a-new-long-password');
  await page.getByRole('button', { name: 'Şifreyi değiştir' }).click();

  await expect(page.getByRole('status')).toHaveText(
    'Şifreniz değişti. Tüm cihazlarınızda yeni şifrenizle giriş yapın.',
  );
  expect(api.requestsTo('POST', '/api/v1/auth/password-reset/confirm')[0]?.body).toEqual({
    token: api.accountLinkToken,
    password: 'a-new-long-password',
  });
});

test('an old reset link says what to do', async ({ page }) => {
  await page.goto(
    'reset-password#0198a1f2000070008000000000000f02.b2xkLWxpbmstc2VjcmV0LXNlY3JldC1zZWNyZXQtc2VjcmV0',
  );
  await page.getByRole('textbox', { name: 'Yeni şifre' }).fill('a-new-long-password');
  await page.getByRole('button', { name: 'Şifreyi değiştir' }).click();

  await expect(page.getByRole('alert')).toHaveText(
    'Bu bağlantının süresi dolmuş. Yeni bir bağlantı isteyin.',
  );
});

test('an unconfirmed owner is reminded, can ask again, and the link confirms the address', async ({
  page,
  api,
}) => {
  api.emailVerified = false;
  await signIn(page);

  const reminder = page.getByRole('status').filter({ hasText: 'adresinizi doğrulayın' });
  await expect(reminder).toContainText(`owner@${slug}.test`);
  await reminder.getByRole('button', { name: 'Doğrulama e-postasını tekrar gönder' }).click();
  await expect(page.getByText('Doğrulama e-postası gönderildi.')).toBeVisible();
  expect(api.requestsTo('POST', '/api/v1/me/email-verification')).toHaveLength(1);

  await page.goto(`verify-email#${api.accountLinkToken}`);
  await expect(page.getByRole('status')).toHaveText('E-posta adresiniz doğrulandı.');
  await expect(page.getByRole('link', { name: 'Panele devam et' })).toHaveAttribute('href', `/${slug}/menu`);
});

test('the other businesses of the person are one click away', async ({ page }) => {
  await signIn(page);

  await page.getByRole('button', { name: /Deniz Yılmaz/ }).click();
  const other = page.getByRole('menuitem', { name: /Boğaziçi Balıkçısı/ });
  await expect(other).toContainText('Personel');
  await expect(page.getByRole('menuitem', { name: /Kadıköy Burger Lab/ })).toHaveCount(0);

  await other.click();
  await expect(page).toHaveURL(/\/bogazici-balikcisi\/sign-in/);
});
