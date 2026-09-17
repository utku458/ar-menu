import { fileURLToPath } from 'node:url';

import type { Page } from '@playwright/test';

import { ids } from './support/fake-api.ts';
import { expect, signIn, test } from './support/test.ts';

const model = fileURLToPath(
  new URL('../../../../assets/demo/models/sea-bass.scene-viewer.glb', import.meta.url),
);

async function openModelTab(page: Page, dish = 'Trüflü Mantar Burger') {
  await signIn(page);
  await page.getByRole('button', { name: `İşlemler: ${dish}` }).click();
  await page.getByRole('menuitem', { name: 'Düzenle' }).click();
  await page.getByRole('tab', { name: '3D model' }).click();
  return page.getByRole('dialog');
}

function modelViewerLoaded(page: Page) {
  return page.evaluate(() => (document.querySelector('model-viewer') as { loaded?: boolean } | null)?.loaded);
}

test('one uploaded GLB is processed in the background and goes live with a report', async ({ page, api }) => {
  const dialog = await openModelTab(page);

  await dialog
    .getByRole('region', { name: '3D model (GLB)' })
    .locator('input[type=file]')
    .setInputFiles(model);

  // Straight to storage, then handed to processing: nothing is published by the browser.
  await expect(dialog.getByText('Model işlenmek için sırada…')).toBeVisible();
  expect(api.uploads).toEqual([
    { contentType: 'model/gltf-binary', size: expect.any(Number) as unknown as number },
  ]);
  expect(
    api.requestsTo('POST', `/api/v1/manage/menu/items/${ids.truffleBurger}/ar-model/processing`),
  ).toHaveLength(1);
  expect(api.requests.filter((request) => request.path.endsWith('/publish'))).toEqual([]);

  await expect(page.getByText('Yeni 3D model yayımlandı. Misafirler hemen görebilir.')).toBeVisible({
    timeout: 10_000,
  });
  const report = dialog.getByRole('region', { name: 'Son işleme' });
  await expect(report).toContainText('İndirme: 17,5 MB → 1,3 MB (%93 daha küçük)');
  await expect(report).toContainText('Üçgen: 412.000 → 99.620');
  await expect(report).toContainText('Masadaki boyut: 18,2 × 17,9 × 11,4 cm');
  await expect(report).toContainText('Model, telefonlarda akıcı dönmesi için sadeleştirildi.');

  // The preview is the published, Meshopt compressed model, decoded without any third-party request.
  await expect.poll(() => modelViewerLoaded(page), { timeout: 15_000 }).toBe(true);
});

test('a model processing rejects is explained, and the dish keeps its model', async ({ page, api }) => {
  api.processingOutcome = { failureCode: 'model.texture_unsupported' };
  const dialog = await openModelTab(page);

  await dialog
    .getByRole('region', { name: '3D model (GLB)' })
    .locator('input[type=file]')
    .setInputFiles(model);

  const failure = dialog.getByRole('alert');
  await expect(failure).toContainText('Model yayımlanamadı', { timeout: 10_000 });
  await expect(failure).toContainText(
    'Modelin bir dokusu okunamadı ya da KTX2 biçiminde. PNG, JPEG veya WebP kullanın.',
  );
  await expect(dialog.getByText('Henüz 3D model yok')).toBeVisible();
});

test('a generated poster can be replaced, keeping every other file of the model', async ({ page, api }) => {
  const dialog = await openModelTab(page);
  await dialog
    .getByRole('region', { name: '3D model (GLB)' })
    .locator('input[type=file]')
    .setInputFiles(model);
  await expect(page.getByText('Yeni 3D model yayımlandı. Misafirler hemen görebilir.')).toBeVisible({
    timeout: 10_000,
  });

  await dialog.getByText('Oluşturulan dosyaları değiştir').click();
  await dialog
    .getByRole('region', { name: 'Poster' })
    .locator('input[type=file]')
    .setInputFiles({ name: 'burger.webp', mimeType: 'image/webp', buffer: Buffer.from('RIFF0000WEBPVP8 ') });

  await expect(page.getByText('Dosya değiştirildi.')).toBeVisible();
  const [attach] = api.requestsTo('PUT', `/api/v1/manage/menu/items/${ids.truffleBurger}/ar-model`);
  expect(attach?.body).toEqual({
    glbPath: expect.stringMatching(/\.glb$/) as unknown as string,
    sceneViewerGlbPath: expect.stringMatching(/\.scene-viewer\.glb$/) as unknown as string,
    usdzPath: expect.stringMatching(/\.usdz$/) as unknown as string,
    posterPath: expect.stringMatching(/^tenants\/aa\/assets\/.+\.webp$/) as unknown as string,
  });
});

test('a file the API would refuse is stopped before a byte is uploaded', async ({ page, api }) => {
  const dialog = await openModelTab(page);

  await dialog
    .getByRole('region', { name: '3D model (GLB)' })
    .locator('input[type=file]')
    .setInputFiles({ name: 'burger.gltf', mimeType: 'model/gltf+json', buffer: Buffer.from('{}') });

  await expect(dialog.getByRole('alert')).toHaveText('Bu alan bu dosya türünü kabul etmiyor.');
  expect(api.requestsTo('POST', '/api/v1/manage/assets/uploads')).toEqual([]);
});
