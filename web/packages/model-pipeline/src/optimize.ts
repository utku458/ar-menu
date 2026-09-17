import type { Document } from '@gltf-transform/core';
import {
  EXTMeshoptCompression,
  KHRDracoMeshCompression,
  KHRMeshQuantization,
} from '@gltf-transform/extensions';
import {
  cloneDocument,
  dedup,
  dequantize,
  flatten,
  join,
  meshopt,
  prune,
  resample,
  simplify,
  weld,
} from '@gltf-transform/functions';
import { MeshoptEncoder, MeshoptSimplifier } from 'meshoptimizer';

import { ModelRejectedError } from './rejection.ts';
import { statsOf } from './stats.ts';
import { encodeTextures } from './textures.ts';

export interface PipelineLimits {
  /**
   * Triangle budget of a dish. Google recommends at most 100,000 for Scene Viewer, and a phone keeps turning such a
   * model smoothly in the page.
   */
  readonly maxTriangles: number;
  /** Largest source file, in bytes. */
  readonly maxSourceBytes: number;
  /** Beyond this, simplification would take minutes: the file is not a dish but a scene. */
  readonly maxSourceTriangles: number;
  /** Scene Viewer's texture limit, and plenty for a plate seen from half a meter. */
  readonly maxTextureSize: number;
}

export const defaultLimits: PipelineLimits = {
  maxTriangles: 100_000,
  maxSourceBytes: 64 * 1024 * 1024,
  maxSourceTriangles: 4_000_000,
  maxTextureSize: 2048,
};

export interface PreparedModel {
  readonly document: Document;
  readonly simplified: boolean;
}

// Compression in the source is decoded on read; what the outputs use is decided per output.
const decodedExtensions = new Set<string>([
  KHRDracoMeshCompression.EXTENSION_NAME,
  EXTMeshoptCompression.EXTENSION_NAME,
  KHRMeshQuantization.EXTENSION_NAME,
]);

/**
 * Lossless cleanup shared by every output, then simplification down to the triangle budget when needed. The result
 * is plain glTF: float geometry, no compression extensions.
 */
export async function prepare(document: Document, limits: PipelineLimits): Promise<PreparedModel> {
  const { triangles } = statsOf(document);
  if (triangles === 0) {
    throw new ModelRejectedError('model.empty', 'The model has no triangles.');
  }
  if (triangles > limits.maxSourceTriangles) {
    throw new ModelRejectedError(
      'model.too_complex',
      `The model has ${triangles} triangles; at most ${limits.maxSourceTriangles} can be processed.`,
    );
  }

  await document.transform(dequantize(), dedup(), flatten(), join({ keepNamed: false }), weld(), resample());

  for (const extension of document.getRoot().listExtensionsUsed()) {
    if (decodedExtensions.has(extension.extensionName)) {
      extension.dispose();
    }
  }

  const simplified = triangles > limits.maxTriangles;
  if (simplified) {
    await MeshoptSimplifier.ready;
    await document.transform(
      // The ratio targets the budget; the error bound (1% of the model's size) keeps the silhouette.
      simplify({ simplifier: MeshoptSimplifier, ratio: limits.maxTriangles / triangles, error: 0.01 }),
    );
  }

  await document.transform(prune());
  return { document, simplified };
}

/**
 * For Android Scene Viewer, which opens the file itself and supports no compression extensions: float geometry and
 * JPEG (or PNG where transparency is used) textures.
 */
export async function forSceneViewer(prepared: Document, limits: PipelineLimits): Promise<Document> {
  const document = cloneDocument(prepared);
  await encodeTextures(document, {
    maxSize: limits.maxTextureSize,
    format: (isOpaque) => (isOpaque ? 'jpeg' : 'png'),
  });
  return document;
}

/**
 * For the in-page viewer and WebXR: Meshopt-compressed, quantized geometry and WebP textures, the smallest download
 * three.js decodes with the decoder already in the 3D bundle.
 */
export async function forWeb(
  prepared: Document,
  limits: PipelineLimits,
): Promise<{ document: Document; downscaled: boolean }> {
  const document = cloneDocument(prepared);
  const downscaled = await encodeTextures(document, { maxSize: limits.maxTextureSize, format: () => 'webp' });

  await MeshoptEncoder.ready;
  await document.transform(meshopt({ encoder: MeshoptEncoder, level: 'medium' }));
  return { document, downscaled };
}
