import { ids } from './support/menus.ts';
import { expect, test } from './support/test.ts';

test('the menu reports what guests do, once per thing and with nothing about the guest', async ({
  page,
  api,
}) => {
  const consoleLogs: string[] = [];
  page.on('console', (message) => {
    if (message.type() === 'log') {
      consoleLogs.push(message.text());
    }
  });
  const eventRequests: { cookie: string | undefined; credentials: boolean }[] = [];
  page.on('request', (request) => {
    if (request.method() === 'POST' && request.url().endsWith('/events')) {
      eventRequests.push({
        cookie: request.headers().cookie,
        credentials: request.headers().authorization !== undefined,
      });
    }
  });

  await page.goto('kadikoy-burger-lab');
  await page.getByRole('button', { name: 'Klasik Smash Burger' }).click();
  await expect
    .poll(
      () =>
        page.evaluate(() => (document.querySelector('model-viewer') as { loaded?: boolean } | null)?.loaded),
      {
        timeout: 15_000,
      },
    )
    .toBe(true);
  await page.keyboard.press('Escape');
  await page.getByRole('button', { name: 'Klasik Smash Burger' }).click();

  const reported = () =>
    api.eventBatches.flatMap((batch) => batch.events as { type: string; itemId: string | null }[]);
  await expect.poll(reported, { timeout: 10_000 }).toHaveLength(3);
  expect(reported()).toEqual([
    { type: 'menu_viewed', itemId: null },
    { type: 'dish_opened', itemId: ids.smashBurger },
    { type: 'model_viewed', itemId: ids.smashBurger },
  ]);
  expect(api.eventBatches.map((batch) => batch.tenant)).toEqual(['kadikoy-burger-lab']);
  expect(eventRequests).toEqual([{ cookie: undefined, credentials: false }]);
  // Guests' consoles stay quiet: the viewer's leftover debug logging is stripped at build time.
  expect(consoleLogs).toEqual([]);
});
