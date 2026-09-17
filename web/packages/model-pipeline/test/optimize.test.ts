import sharp from 'sharp';
import { describe, expect, it } from 'vitest';

import { gltfIO } from '../src/gltf-io.ts';
import { defaultLimits, forSceneViewer, forWeb, prepare } from '../src/optimize.ts';
import { ModelRejectedError } from '../src/rejection.ts';
import { statsOf } from '../src/stats.ts';
import { dracoGlbOf, glbOf, only, requiredExtensionsOf, texturedDish } from './fixtures.ts';

async function read(glb: Uint8Array) {
  return (await gltfIO()).readBinary(glb);
}

async function textureOf(glb: Uint8Array) {
  const texture = only((await read(glb)).getRoot().listTextures());
  return { mimeType: texture.getMimeType(), ...(await sharp(texture.getImage() ?? undefined).metadata()) };
}

describe('the web model', () => {
  it('is Meshopt compressed, quantized and textured in WebP, without losing a triangle', async () => {
    const source = await texturedDish();
    const triangles = statsOf(source).triangles;

    const { document } = await prepare(source, defaultLimits);
    const glb = await glbOf((await forWeb(document, defaultLimits)).document);

    expect(requiredExtensionsOf(glb).sort()).toEqual([
      'EXT_meshopt_compression',
      'EXT_texture_webp',
      'KHR_mesh_quantization',
    ]);
    expect(statsOf(await read(glb)).triangles).toBe(triangles);
    expect(await textureOf(glb)).toMatchObject({ mimeType: 'image/webp', format: 'webp' });
  });

  it('is much smaller than the plain model Scene Viewer gets', async () => {
    const { document } = await prepare(await texturedDish({ segments: 256 }), defaultLimits);

    const web = await glbOf((await forWeb(document, defaultLimits)).document);
    const sceneViewer = await glbOf(await forSceneViewer(document, defaultLimits));

    expect(web.byteLength).toBeLessThan(sceneViewer.byteLength * 0.7);
  });

  it('downscales textures to the limit, keeping their aspect ratio', async () => {
    const { document } = await prepare(
      await texturedDish({ textureWidth: 3000, textureHeight: 1500 }),
      defaultLimits,
    );

    const web = await forWeb(document, defaultLimits);

    expect(web.downscaled).toBe(true);
    expect(await textureOf(await glbOf(web.document))).toMatchObject({ width: 2048, height: 1024 });
  });
});

describe('the Scene Viewer model', () => {
  it('needs no extension Scene Viewer lacks: float geometry and JPEG textures', async () => {
    const { document } = await prepare(await texturedDish(), defaultLimits);

    const glb = await glbOf(await forSceneViewer(document, defaultLimits));

    expect(requiredExtensionsOf(glb)).toEqual([]);
    expect(await textureOf(glb)).toMatchObject({ mimeType: 'image/jpeg', format: 'jpeg' });
  });

  it('keeps transparency as PNG', async () => {
    const { document } = await prepare(await texturedDish({ transparent: true }), defaultLimits);

    const glb = await glbOf(await forSceneViewer(document, defaultLimits));

    expect(await textureOf(glb)).toMatchObject({ mimeType: 'image/png', hasAlpha: true });
  });
});

describe('preparing a source', () => {
  it('simplifies a scan to the triangle budget', async () => {
    const source = await texturedDish({ segments: 512 });
    expect(statsOf(source).triangles).toBeGreaterThan(250_000);

    const prepared = await prepare(source, defaultLimits);

    expect(prepared.simplified).toBe(true);
    expect(statsOf(prepared.document).triangles).toBeLessThanOrEqual(defaultLimits.maxTriangles * 1.05);
  });

  it('leaves a model within budget untouched', async () => {
    const source = await texturedDish();
    const triangles = statsOf(source).triangles;

    const prepared = await prepare(source, defaultLimits);

    expect(prepared.simplified).toBe(false);
    expect(statsOf(prepared.document).triangles).toBe(triangles);
  });

  it('decodes Draco compressed geometry, which exporters often produce', async () => {
    const glb = await dracoGlbOf(await texturedDish());
    expect(requiredExtensionsOf(glb)).toContain('KHR_draco_mesh_compression');

    const { document } = await prepare(await read(glb), defaultLimits);
    const sceneViewer = await glbOf(await forSceneViewer(document, defaultLimits));

    expect(requiredExtensionsOf(sceneViewer)).toEqual([]);
    expect(statsOf(await read(sceneViewer)).triangles).toBe(statsOf(await texturedDish()).triangles);
  });

  it('refuses a model without triangles', async () => {
    const source = await texturedDish();
    for (const mesh of source.getRoot().listMeshes()) {
      mesh.dispose();
    }

    await expect(prepare(source, defaultLimits)).rejects.toMatchObject({ code: 'model.empty' });
  });

  it('refuses a model far beyond any phone', async () => {
    await expect(
      prepare(await texturedDish({ segments: 128 }), { ...defaultLimits, maxSourceTriangles: 1000 }),
    ).rejects.toMatchObject({ code: 'model.too_complex' });
  });

  it('refuses KTX2 textures, which every device would need a transcoder for', async () => {
    const source = await texturedDish();
    only(source.getRoot().listTextures()).setMimeType('image/ktx2');
    const { document } = await prepare(source, defaultLimits);

    const rejection = forWeb(document, defaultLimits);

    await expect(rejection).rejects.toBeInstanceOf(ModelRejectedError);
    await expect(rejection).rejects.toMatchObject({ code: 'model.texture_unsupported' });
  });
});
