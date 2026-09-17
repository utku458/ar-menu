import sharp from 'sharp';
import { afterAll, beforeAll, describe, expect, it } from 'vitest';

import { defaultLimits, ModelRejectedError, ModelRenderer, processModel } from '../src/index.ts';
import { dracoGlbOf, glbOf, requiredExtensionsOf, texturedDish } from './fixtures.ts';

// Rendering needs Chrome, like the end-to-end tests; CI images provide it.
let renderer: ModelRenderer;

beforeAll(async () => {
  // Shorter than the tests' own timeouts: a render that hangs on a busy machine must end as a rejection, not a test
  // timeout.
  renderer = await ModelRenderer.launch({ channel: 'chrome', timeoutMs: 30_000 });
});

afterAll(async () => {
  await renderer.close();
});

/** First entry of a zip archive: its name and whether it is stored uncompressed, as AR Quick Look requires. */
function firstZipEntry(zip: Uint8Array) {
  const view = new DataView(zip.buffer, zip.byteOffset, zip.byteLength);
  const nameLength = view.getUint16(26, true);
  return {
    signature: view.getUint32(0, true),
    compression: view.getUint16(8, true),
    name: new TextDecoder().decode(zip.subarray(30, 30 + nameLength)),
  };
}

/** A GLB with a JSON chunk only. */
function glbWithJson(json: object): Uint8Array {
  const text = new TextEncoder().encode(JSON.stringify(json));
  const padded = new Uint8Array(Math.ceil(text.byteLength / 4) * 4).fill(0x20);
  padded.set(text);

  const glb = new Uint8Array(20 + padded.byteLength);
  const view = new DataView(glb.buffer);
  view.setUint32(0, 0x46546c67, true);
  view.setUint32(4, 2, true);
  view.setUint32(8, glb.byteLength, true);
  view.setUint32(12, padded.byteLength, true);
  view.setUint32(16, 0x4e4f534a, true);
  glb.set(padded, 20);
  return glb;
}

describe('processing a model', () => {
  it('produces every file a device needs, and a report of what changed', { timeout: 120_000 }, async () => {
    const source = await dracoGlbOf(
      await texturedDish({ segments: 512, textureWidth: 4096, textureHeight: 4096 }),
    );

    const processed = await processModel(source, renderer);

    expect(requiredExtensionsOf(processed.model)).toContain('EXT_meshopt_compression');
    expect(requiredExtensionsOf(processed.sceneViewerModel)).toEqual([]);

    expect(firstZipEntry(processed.appleModel)).toEqual({
      signature: 0x04034b50,
      compression: 0,
      name: 'model.usda',
    });

    const poster = await sharp(processed.poster).metadata();
    expect(poster).toMatchObject({ format: 'webp', width: 768, height: 768 });
    // A rendered dish, not an empty frame: a blank transparent image compresses to almost nothing.
    expect(processed.poster.byteLength).toBeGreaterThan(5_000);

    const { report } = processed;
    expect(report.source).toMatchObject({ maxTextureSize: 4096, textures: 1, materials: 1 });
    expect(report.source.triangles).toBeGreaterThan(250_000);
    expect(report.optimized.triangles).toBeLessThanOrEqual(defaultLimits.maxTriangles * 1.05);
    expect(report.optimized.maxTextureSize).toBe(2048);
    expect(report.files).toEqual({
      source: source.byteLength,
      model: processed.model.byteLength,
      sceneViewerModel: processed.sceneViewerModel.byteLength,
      appleModel: processed.appleModel.byteLength,
      poster: processed.poster.byteLength,
    });
    expect(report.dimensions.width).toBeCloseTo(0.24, 2);
    expect(report.warnings).toEqual(['model.simplified', 'model.textures_downscaled']);
  });

  it('warns about a model exported in the wrong unit', { timeout: 60_000 }, async () => {
    // A 24 cm dish exported in centimeters: 24 "meters" wide.
    const processed = await processModel(await glbOf(await texturedDish({ radius: 12 })), renderer);

    expect(processed.report.warnings).toContain('model.unusual_size');
  });

  it('refuses a file that is not a glTF binary', async () => {
    const rejection = processModel(new TextEncoder().encode('not a model'), renderer);

    await expect(rejection).rejects.toBeInstanceOf(ModelRejectedError);
    await expect(rejection).rejects.toMatchObject({ code: 'model.unreadable' });
  });

  it('reports a model the viewer cannot load as a render failure', { timeout: 60_000 }, async () => {
    const glb = glbWithJson({
      asset: { version: '2.0' },
      buffers: [{ uri: 'https://example.com/geometry.bin', byteLength: 12 }],
      bufferViews: [{ buffer: 0, byteLength: 12 }],
      accessors: [
        { bufferView: 0, componentType: 5126, count: 1, type: 'VEC3', min: [0, 0, 0], max: [1, 1, 1] },
      ],
      meshes: [{ primitives: [{ attributes: { POSITION: 0 } }] }],
      nodes: [{ mesh: 0 }],
      scenes: [{ nodes: [0] }],
      scene: 0,
    });

    await expect(renderer.render(glb)).rejects.toMatchObject({ code: 'model.render_failed' });
  });
});
