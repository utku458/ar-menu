import { expect, type Locator, type Page, test as base } from '@playwright/test';

import { FakeApi, password, slug } from './fake-api.ts';

export const test = base.extend<{ api: FakeApi; contentSecurityPolicy: string[] }>({
  // Pages are served with the production security headers (vite preview); nothing the app does may break its policy.
  contentSecurityPolicy: [
    async ({ page }, use) => {
      const violations: string[] = [];
      const unprotectedPages: string[] = [];
      page.on('console', (message) => {
        if (message.text().includes('Content Security Policy')) {
          violations.push(message.text());
        }
      });
      page.on('response', (response) => {
        if (
          response.request().resourceType() === 'document' &&
          response.headers()['content-security-policy'] === undefined
        ) {
          unprotectedPages.push(response.url());
        }
      });

      await use(violations);

      expect(unprotectedPages, 'pages served without a content security policy').toEqual([]);
      expect(violations, 'content security policy violations').toEqual([]);
    },
    { auto: true },
  ],

  api: [
    async ({ page }, use) => {
      const api = new FakeApi(page);
      await api.install();
      await use(api);
    },
    { auto: true },
  ],
});

export { expect };

export async function signIn(page: Page, role: 'owner' | 'staff' = 'owner'): Promise<void> {
  await page.goto(`${slug}/menu`);
  await page.getByRole('textbox', { name: 'Kullanıcı adı' }).fill(role);
  await page.getByRole('textbox', { name: 'Şifre' }).fill(password);
  await page.getByRole('button', { name: 'Giriş yap' }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'Menü' })).toBeVisible();
}

/**
 * Clicks a React Aria switch or radio the way a pointer user does: on its visible label. The native input is visually
 * hidden (1px), so Playwright's hit-target check would reject a click aimed at the input itself.
 */
export async function toggle(control: Locator): Promise<void> {
  await control.locator('xpath=ancestor::label[1]').click();
}
