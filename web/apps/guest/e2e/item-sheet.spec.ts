import type { Page } from '@playwright/test';

import { ids } from './support/menus.ts';
import { expect, test } from './support/test.ts';

type ViewerElement = HTMLElement & { loaded?: boolean; canActivateAR?: boolean };

function modelLoaded(page: Page): Promise<boolean> {
  return page.evaluate(() => document.querySelector<ViewerElement>('model-viewer')?.loaded === true);
}

test('the 3D stack is downloaded only once a dish with a model is opened', async ({ page }) => {
  const arChunkRequests: string[] = [];
  page.on('request', (request) => {
    if (request.url().includes('/assets/ar-viewer-')) {
      arChunkRequests.push(request.url());
    }
  });

  await page.goto('kadikoy-burger-lab');
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();

  expect(arChunkRequests).toEqual([]);
  expect(await page.evaluate(() => customElements.get('model-viewer') === undefined)).toBe(true);

  await page.getByRole('button', { name: 'Klasik Smash Burger' }).click();

  await expect(page.getByRole('dialog', { name: 'Klasik Smash Burger' })).toBeVisible();
  await expect.poll(() => modelLoaded(page), { timeout: 15_000 }).toBe(true);
  expect(arChunkRequests).toHaveLength(1);
});

test('the back button closes the sheet and keeps the reading position', async ({ page }) => {
  await page.goto('kadikoy-burger-lab');
  const dessert = page.getByRole('button', { name: 'Kazandibi' });
  await dessert.scrollIntoViewIfNeeded();
  const readingPosition = await page.evaluate(() => window.scrollY);

  await dessert.click();
  await expect(page.getByRole('dialog', { name: 'Kazandibi' })).toBeVisible();
  await expect(page).toHaveURL(/item=/);

  await page.goBack();

  await expect(page.getByRole('dialog')).toBeHidden();
  await expect(page).not.toHaveURL(/item=/);
  expect(Math.abs((await page.evaluate(() => window.scrollY)) - readingPosition)).toBeLessThanOrEqual(2);
});

test('a link to a dish opens its sheet, and Escape closes it', async ({ page }) => {
  await page.goto(`kadikoy-burger-lab?item=${ids.truffleBurger}`);
  const sheet = page.getByRole('dialog', { name: 'Trüflü Mantar Burger' });

  await expect(sheet).toBeVisible();
  await expect(sheet).toContainText('Tükendi');
  await expect(sheet.getByRole('button', { name: 'Kapat' })).toBeFocused();

  await page.keyboard.press('Escape');

  await expect(sheet).toBeHidden();
  await expect(page).not.toHaveURL(/item=/);
});

test('phones get the AR button; desktops are invited to use a phone', async ({ page, isMobile }) => {
  await page.goto(`kadikoy-burger-lab?item=${ids.smashBurger}`);
  const sheet = page.getByRole('dialog', { name: 'Klasik Smash Burger' });
  await expect.poll(() => modelLoaded(page), { timeout: 15_000 }).toBe(true);

  const arButton = sheet.getByRole('button', { name: 'Masanızda görün' });
  const phoneHint = sheet.getByText('Yemeği masanızda görmek için bu menüyü telefonunuzda açın.');

  if (isMobile) {
    await expect(arButton).toBeVisible();
    await expect(phoneHint).toBeHidden();
  } else {
    await expect(arButton).toBeHidden();
    await expect(phoneHint).toBeVisible();
  }
});

function modelRequests(page: Page): string[] {
  const models: string[] = [];
  page.on('request', (request) => {
    if (request.url().endsWith('.glb')) {
      models.push(new URL(request.url()).pathname);
    }
  });
  return models;
}

test('each device downloads the one model file its AR can open', async ({ page, isMobile }) => {
  const models = modelRequests(page);

  await page.goto(`kadikoy-burger-lab?item=${ids.smashBurger}`);
  await expect.poll(() => modelLoaded(page), { timeout: 15_000 }).toBe(true);

  // The phone profile is an Android browser without WebXR, whose AR opens Scene Viewer: it needs the plain model.
  expect(models).toEqual([
    isMobile ? '/demo/models/smash-burger.scene-viewer.glb' : '/demo/models/smash-burger.glb',
  ]);
});

test('Android browsers with WebXR get the compressed model', async ({ page, isMobile }) => {
  test.skip(!isMobile, 'Only Android picks between the two files.');
  await page.addInitScript(() => {
    Object.defineProperty(navigator, 'xr', {
      value: { isSessionSupported: (mode: string) => Promise.resolve(mode === 'immersive-ar') },
    });
  });
  const models = modelRequests(page);

  await page.goto(`kadikoy-burger-lab?item=${ids.smashBurger}`);
  await expect.poll(() => models.length, { timeout: 15_000 }).toBeGreaterThan(0);

  expect(models).toEqual(['/demo/models/smash-burger.glb']);
});
