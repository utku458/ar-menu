import { Document, type Material, NodeIO } from '@gltf-transform/core';
import { DoubleSide, type Mesh, MeshStandardMaterial, type Object3D } from 'three';

export interface GlbModel {
  readonly bytes: Uint8Array;
}

/**
 * Exports a three.js model as a plain binary glTF, the way a 3D tool would. Optimizing it is the asset pipeline's job,
 * exactly as for a business's upload.
 */
export async function toGlb(model: Object3D): Promise<GlbModel> {
  const document = new Document();
  document.getRoot().getAsset().generator = 'ArMenu demo assets';
  const buffer = document.createBuffer();
  const scene = document.createScene(model.name);
  const materials = new Map<MeshStandardMaterial, Material>();

  model.updateMatrixWorld(true);
  model.traverse((object) => {
    if (!isMesh(object) || !(object.material instanceof MeshStandardMaterial)) {
      return;
    }

    const geometry = object.geometry.clone().applyMatrix4(object.matrixWorld);
    const primitive = document
      .createPrimitive()
      .setAttribute('POSITION', vec3(document, geometry.getAttribute('position').array).setBuffer(buffer))
      .setAttribute('NORMAL', vec3(document, geometry.getAttribute('normal').array).setBuffer(buffer))
      .setMaterial(materialFor(document, materials, object.material));

    if (geometry.index !== null) {
      primitive.setIndices(
        document
          .createAccessor()
          .setType('SCALAR')
          .setArray(new Uint32Array(geometry.index.array))
          .setBuffer(buffer),
      );
    }

    const mesh = document.createMesh(object.name).addPrimitive(primitive);
    scene.addChild(document.createNode(object.name).setMesh(mesh));
  });

  return { bytes: await new NodeIO().writeBinary(document) };
}

// `instanceof Mesh` narrows to Mesh<any>; the flag three.js sets keeps the default, typed generics.
function isMesh(object: Object3D): object is Mesh {
  return (object as Partial<Mesh>).isMesh === true;
}

function vec3(document: Document, array: ArrayLike<number>) {
  return document.createAccessor().setType('VEC3').setArray(new Float32Array(array));
}

function materialFor(
  document: Document,
  cache: Map<MeshStandardMaterial, Material>,
  source: MeshStandardMaterial,
): Material {
  let material = cache.get(source);
  if (material === undefined) {
    // three.js keeps colors in linear space, which is what glTF expects.
    material = document
      .createMaterial(source.name)
      .setBaseColorFactor([source.color.r, source.color.g, source.color.b, 1])
      .setRoughnessFactor(source.roughness)
      .setMetallicFactor(source.metalness)
      .setDoubleSided(source.side === DoubleSide);
    cache.set(source, material);
  }

  return material;
}
