import { Document, NodeIO } from '@gltf-transform/core';
import { KHRDracoMeshCompression } from '@gltf-transform/extensions';
import { draco } from '@gltf-transform/functions';
import draco3d from 'draco3dgltf';
import sharp from 'sharp';

import { gltfIO } from '../src/gltf-io.ts';

export interface DishOptions {
  /** Longitude segments of the dome; triangles = 2 × segments × (segments / 2). */
  readonly segments?: number;
  readonly textureWidth?: number;
  readonly textureHeight?: number;
  readonly transparent?: boolean;
  readonly radius?: number;
}

/** A textured dome standing in for a scanned dish: UVs, normals and a noisy (photo-like, hard to compress) texture. */
export async function texturedDish({
  segments = 64,
  textureWidth = 1024,
  textureHeight = 1024,
  transparent = false,
  radius = 0.12,
}: DishOptions = {}): Promise<Document> {
  const document = new Document();
  const buffer = document.createBuffer();
  const rings = segments / 2;

  const positions: number[] = [];
  const normals: number[] = [];
  const uvs: number[] = [];
  for (let ring = 0; ring <= rings; ring++) {
    // Starting just off the pole keeps every triangle non-degenerate, so encoders cannot drop any.
    const polar = 0.05 + (ring / rings) * (Math.PI / 2 - 0.05);
    for (let segment = 0; segment <= segments; segment++) {
      const azimuth = (segment / segments) * Math.PI * 2;
      const x = Math.sin(polar) * Math.cos(azimuth);
      const y = Math.cos(polar);
      const z = Math.sin(polar) * Math.sin(azimuth);
      positions.push(x * radius, y * radius * 0.6, z * radius);
      normals.push(x, y, z);
      uvs.push(segment / segments, ring / rings);
    }
  }

  const indices: number[] = [];
  const row = segments + 1;
  for (let ring = 0; ring < rings; ring++) {
    for (let segment = 0; segment < segments; segment++) {
      const a = ring * row + segment;
      indices.push(a, a + 1, a + row, a + 1, a + row + 1, a + row);
    }
  }

  const accessor = (
    type: 'VEC2' | 'VEC3' | 'SCALAR',
    array: Float32Array<ArrayBuffer> | Uint32Array<ArrayBuffer>,
  ) => document.createAccessor().setType(type).setArray(array).setBuffer(buffer);

  const image = await sharp({
    create: {
      width: textureWidth,
      height: textureHeight,
      channels: transparent ? 4 : 3,
      background: { r: 190, g: 120, b: 60, alpha: transparent ? 0.5 : 1 },
      noise: { type: 'gaussian', mean: 128, sigma: 40 },
    },
  })
    .png()
    .toBuffer();

  const texture = document
    .createTexture('crust')
    .setImage(image)
    .setMimeType('image/png')
    .setURI('crust.png');
  const material = document
    .createMaterial('crust')
    .setBaseColorTexture(texture)
    .setAlphaMode(transparent ? 'BLEND' : 'OPAQUE');

  const primitive = document
    .createPrimitive()
    .setAttribute('POSITION', accessor('VEC3', new Float32Array(positions)))
    .setAttribute('NORMAL', accessor('VEC3', new Float32Array(normals)))
    .setAttribute('TEXCOORD_0', accessor('VEC2', new Float32Array(uvs)))
    .setIndices(accessor('SCALAR', new Uint32Array(indices)))
    .setMaterial(material);

  const mesh = document.createMesh('dish').addPrimitive(primitive);
  document.createScene('dish').addChild(document.createNode('dish').setMesh(mesh));
  return document;
}

export async function glbOf(document: Document): Promise<Uint8Array> {
  return (await gltfIO()).writeBinary(document);
}

/** The dish exported with Draco compression, as Blender's "Compression" option does. */
export async function dracoGlbOf(document: Document): Promise<Uint8Array> {
  const io = new NodeIO()
    .registerExtensions([KHRDracoMeshCompression])
    .registerDependencies({ 'draco3d.encoder': await draco3d.createEncoderModule() });
  await document.transform(draco());
  return io.writeBinary(document);
}

export function requiredExtensionsOf(glb: Uint8Array): string[] {
  const view = new DataView(glb.buffer, glb.byteOffset, glb.byteLength);
  const jsonLength = view.getUint32(12, true);
  const json = JSON.parse(new TextDecoder().decode(glb.subarray(20, 20 + jsonLength))) as {
    extensionsRequired?: string[];
  };
  return json.extensionsRequired ?? [];
}

/** The single element of a list, failing the test when there is not exactly one. */
export function only<T>(items: readonly T[]): T {
  const [item] = items;
  if (items.length !== 1 || item === undefined) {
    throw new Error(`Expected exactly one item, found ${items.length}.`);
  }
  return item;
}
