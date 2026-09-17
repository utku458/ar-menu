import { NodeIO } from '@gltf-transform/core';
import { ALL_EXTENSIONS } from '@gltf-transform/extensions';
import draco3d from 'draco3dgltf';
import { MeshoptDecoder, MeshoptEncoder } from 'meshoptimizer';

let io: Promise<NodeIO> | undefined;

/**
 * Reads every glTF extension, including Draco and Meshopt compressed geometry that exporters such as Blender produce,
 * and writes Meshopt. Draco is never written: guests would need a decoder served by a third party.
 */
export function gltfIO(): Promise<NodeIO> {
  io ??= (async () => {
    const [dracoDecoder] = await Promise.all([
      draco3d.createDecoderModule(),
      MeshoptDecoder.ready,
      MeshoptEncoder.ready,
    ]);

    return new NodeIO().registerExtensions(ALL_EXTENSIONS).registerDependencies({
      'draco3d.decoder': dracoDecoder,
      'meshopt.decoder': MeshoptDecoder,
      'meshopt.encoder': MeshoptEncoder,
    });
  })();

  return io;
}
