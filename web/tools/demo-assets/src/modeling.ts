import {
  type BufferGeometry,
  DoubleSide,
  LatheGeometry,
  type Material,
  Matrix4,
  Mesh,
  MeshStandardMaterial,
  type Object3D,
  Quaternion,
  Vector2,
  Vector3,
} from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';

/** Profile point of a surface of revolution: distance from the axis and height, in meters. */
export type ProfilePoint = readonly [radius: number, height: number];

/** Deterministic pseudo-random numbers (mulberry32), so regenerated models keep exactly the same shape. */
export function createRandom(seed: number): () => number {
  let state = seed >>> 0;

  return () => {
    state = (state + 0x6d2b79f5) >>> 0;
    let value = state;
    value = Math.imul(value ^ (value >>> 15), value | 1);
    value ^= value + Math.imul(value ^ (value >>> 7), value | 61);
    return ((value ^ (value >>> 14)) >>> 0) / 4294967296;
  };
}

/** A physically based, non-metallic surface: food is dielectric. Colors are sRGB hex values. */
export function surface(
  name: string,
  color: string,
  roughness: number,
  doubleSided = false,
): MeshStandardMaterial {
  const material = new MeshStandardMaterial({ name, color, roughness, metalness: 0 });
  if (doubleSided) {
    material.side = DoubleSide;
  }

  return material;
}

export function lathe(profile: readonly ProfilePoint[], segments = 40): LatheGeometry {
  return new LatheGeometry(
    profile.map(([radius, height]) => new Vector2(radius, height)),
    segments,
  );
}

/** Height of the upper surface of a revolved profile at a given distance from its axis. */
export function topHeightAt(profile: readonly ProfilePoint[], radius: number): number {
  let top = 0;

  for (const [[r0, h0], [r1, h1]] of segmentsOf(profile)) {
    if (radius >= Math.min(r0, r1) && radius <= Math.max(r0, r1)) {
      top = Math.max(top, r0 === r1 ? Math.max(h0, h1) : h0 + ((radius - r0) / (r1 - r0)) * (h1 - h0));
    }
  }

  return top;
}

function* segmentsOf(profile: readonly ProfilePoint[]): Generator<[ProfilePoint, ProfilePoint]> {
  let previous: ProfilePoint | undefined;
  for (const point of profile) {
    if (previous !== undefined) {
      yield [previous, point];
    }
    previous = point;
  }
}

/** Outward normal of the upper surface of a revolved profile, from its slope. */
export function topNormalAt(profile: readonly ProfilePoint[], radius: number, angle: number): Vector3 {
  const delta = 0.0005;
  const slope =
    (topHeightAt(profile, radius + delta) - topHeightAt(profile, Math.max(0, radius - delta))) / (2 * delta);
  return new Vector3(-slope * Math.cos(angle), 1, -slope * Math.sin(angle)).normalize();
}

export function place(
  geometry: BufferGeometry,
  position: Vector3,
  rotation = new Quaternion(),
  scale = new Vector3(1, 1, 1),
) {
  return geometry.applyMatrix4(new Matrix4().compose(position, rotation, scale));
}

/** Many small parts with one material become one draw call. */
export function merge(parts: BufferGeometry[]): BufferGeometry {
  const allIndexed = parts.every((part) => part.index !== null);
  // The typings promise a geometry, but mergeGeometries returns null when the parts are incompatible.
  const merged = mergeGeometries(
    allIndexed ? parts : parts.map((part) => (part.index === null ? part : part.toNonIndexed())),
  ) as BufferGeometry | null;
  if (merged === null) {
    throw new Error('Geometries with different attributes cannot be merged.');
  }

  return merged;
}

export function mesh(parent: Object3D, name: string, geometry: BufferGeometry, material: Material): Mesh {
  // Untextured models carry no texture coordinates: they would only add weight, most of all to USDZ, which is text.
  geometry.deleteAttribute('uv');
  const created = new Mesh(geometry, material);
  created.name = name;
  parent.add(created);
  return created;
}

/** Rotation that tilts a part lying flat so it follows a surface with the given outward normal. */
export function alignedTo(normal: Vector3, yaw: number): Quaternion {
  const up = new Vector3(0, 1, 0);
  const tilt = new Quaternion().setFromUnitVectors(up, normal.clone().normalize());
  return tilt.multiply(new Quaternion().setFromAxisAngle(up, yaw));
}
