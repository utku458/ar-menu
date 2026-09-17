import type { Document, Texture } from '@gltf-transform/core';
import { getBounds, getGLPrimitiveCount } from '@gltf-transform/functions';

export interface ModelStats {
  readonly triangles: number;
  readonly vertices: number;
  readonly materials: number;
  readonly textures: number;
  /** Longest side of the largest texture, in pixels. */
  readonly maxTextureSize: number;
}

/** Size of the model's bounding box, in meters (glTF units): what a guest sees on the table. */
export interface ModelDimensions {
  readonly width: number;
  readonly height: number;
  readonly depth: number;
}

// glTF primitive modes TRIANGLES, TRIANGLE_STRIP and TRIANGLE_FAN; points and lines are not surfaces.
const triangleModes = new Set([4, 5, 6]);

export function statsOf(document: Document): ModelStats {
  const root = document.getRoot();
  const primitives = root.listMeshes().flatMap((mesh) => mesh.listPrimitives());

  return {
    triangles: primitives
      .filter((primitive) => triangleModes.has(primitive.getMode()))
      .reduce((sum, primitive) => sum + getGLPrimitiveCount(primitive), 0),
    vertices: primitives.reduce(
      (sum, primitive) => sum + (primitive.getAttribute('POSITION')?.getCount() ?? 0),
      0,
    ),
    materials: root.listMaterials().length,
    textures: root.listTextures().length,
    maxTextureSize: Math.max(0, ...root.listTextures().map(longestSideOf)),
  };
}

// A malformed image has no size; the texture step rejects it with a proper reason.
function longestSideOf(texture: Texture): number {
  try {
    return Math.max(...(texture.getSize() ?? [0, 0]));
  } catch {
    return 0;
  }
}

export function dimensionsOf(document: Document): ModelDimensions {
  const scene = document.getRoot().getDefaultScene() ?? document.getRoot().listScenes()[0];
  if (scene === undefined) {
    return { width: 0, height: 0, depth: 0 };
  }

  const { min, max } = getBounds(scene);
  const round = (meters: number) => Math.round(meters * 1000) / 1000;
  return { width: round(max[0] - min[0]), height: round(max[1] - min[1]), depth: round(max[2] - min[2]) };
}
