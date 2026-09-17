import { mkdir, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

import { ModelRenderer, processModel } from '@armenu/model-pipeline';
import type { Group } from 'three';

import { seaBass } from './dishes/sea-bass.ts';
import { smashBurger } from './dishes/smash-burger.ts';
import { toGlb } from './gltf.ts';

/** Uploaded to local storage by the API in Development (see StorageInitializer). */
const assetRoot = fileURLToPath(new URL('../../../../assets/', import.meta.url));

// Asset keys match the development seed (src/ArMenu.Infrastructure/Persistence/Seeding/DemoMenus.cs).
const dishes = new Map<string, () => Group>([
  ['smash-burger', smashBurger],
  ['sea-bass', seaBass],
]);

// The demo dishes go through the same pipeline as a business's upload, so they look and load like real ones.
const renderer = await ModelRenderer.launch({ channel: 'chrome' });
try {
  for (const [name, build] of dishes) {
    const source = await toGlb(build());
    const { model, sceneViewerModel, appleModel, poster, report } = await processModel(
      source.bytes,
      renderer,
    );

    await save(`demo/models/${name}.glb`, model, `${report.optimized.triangles} triangles`);
    await save(`demo/models/${name}.scene-viewer.glb`, sceneViewerModel);
    await save(`demo/models/${name}.usdz`, appleModel);
    await save(`demo/posters/${name}.webp`, poster);
    if (report.warnings.length > 0) {
      console.warn(`  ${name}: ${report.warnings.join(', ')}`);
    }
  }
} finally {
  await renderer.close();
}

async function save(key: string, content: Uint8Array, details = ''): Promise<void> {
  const path = join(assetRoot, key);
  await mkdir(dirname(path), { recursive: true });
  await writeFile(path, content);
  console.log(`assets/${key}  ${(content.byteLength / 1024).toFixed(1)} KiB  ${details}`.trimEnd());
}
