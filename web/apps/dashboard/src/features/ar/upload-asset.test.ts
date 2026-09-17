import type { AssetUpload } from '@armenu/api-client';
import { describe, expect, test, vi } from 'vitest';

import { checkFile, contentTypeFor, uploadAsset, uploadModelForProcessing } from './upload-asset.ts';

describe('checkFile', () => {
  test.each([
    ['model', 'burger.GLB', 1000, undefined],
    ['model', 'burger.gltf', 1000, 'asset.content_type_not_allowed'],
    ['model', 'scan.glb', 60 * 1024 * 1024, undefined],
    ['model', 'scan.glb', 65 * 1024 * 1024, 'asset.too_large'],
    ['appleModel', 'burger.usdz', 65 * 1024 * 1024, 'asset.too_large'],
    ['poster', 'burger.webp', 2 * 1024 * 1024, undefined],
    ['poster', 'burger.svg', 100, 'asset.content_type_not_allowed'],
    ['poster', 'empty.png', 0, 'asset.size_invalid'],
  ] as const)('%s %s (%d bytes) → %s', (kind, name, size, code) => {
    expect(checkFile(kind, { name, size })).toBe(code);
  });
});

describe('contentTypeFor', () => {
  test('models get their registered media type regardless of what the browser reports', () => {
    expect(contentTypeFor('model', 'dish.glb')).toBe('model/gltf-binary');
    expect(contentTypeFor('appleModel', 'dish.usdz')).toBe('model/vnd.usdz+zip');
    expect(contentTypeFor('poster', 'dish.JPG')).toBe('image/jpeg');
  });
});

describe('uploading', () => {
  const upload = (contentType: string): AssetUpload => ({
    uploadId: '0198a1f2-0000-7000-8000-000000000001',
    url: 'https://storage.test/staging/abc',
    method: 'PUT',
    headers: { 'Content-Type': contentType },
    expiresAt: '2026-09-14T12:00:00Z',
  });
  const ok = (data: unknown) => ({ data, response: new Response(null, { status: 200 }) });

  test('a poster is uploaded for exactly this file, then published', async () => {
    const published = {
      path: 'tenants/t/assets/abc.webp',
      url: 'https://cdn.test/abc.webp',
      contentType: 'image/webp',
      size: 4,
    };
    const post = vi
      .fn()
      .mockResolvedValueOnce(ok(upload('image/webp')))
      .mockResolvedValueOnce(ok(published));
    const put = vi.fn(() => Promise.resolve());
    const file = new File(['RIFF'], 'burger.webp');

    const result = await uploadAsset({ POST: post }, 'poster', file, { put });

    expect(result).toEqual(published);
    expect(post.mock.calls[0]?.[1]).toMatchObject({
      body: { kind: 'poster', contentType: 'image/webp', size: 4 },
    });
    expect(put).toHaveBeenCalledWith(upload('image/webp'), file, expect.any(Function), undefined);
    expect(post.mock.calls[1]?.[1]).toMatchObject({
      params: { path: { uploadId: upload('image/webp').uploadId } },
      body: { kind: 'poster' },
    });
  });

  test('a model is uploaded, then handed to processing for its item instead of being published', async () => {
    const processing = { id: 'p', status: 'queued' };
    const post = vi
      .fn()
      .mockResolvedValueOnce(ok(upload('model/gltf-binary')))
      .mockResolvedValueOnce(ok(processing));
    const put = vi.fn(() => Promise.resolve());

    const result = await uploadModelForProcessing({ POST: post }, 'item-1', new File(['glTF'], 'scan.glb'), {
      put,
    });

    expect(result).toEqual(processing);
    expect(post.mock.calls[1]?.[0]).toBe('/api/v1/manage/menu/items/{itemId}/ar-model/processing');
    expect(post.mock.calls[1]?.[1]).toMatchObject({
      params: { path: { itemId: 'item-1' } },
      body: { uploadId: upload('model/gltf-binary').uploadId },
    });
  });
});
