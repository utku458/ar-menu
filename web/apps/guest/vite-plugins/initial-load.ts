import { gzipSync } from 'node:zlib';

import type { Plugin, Rollup } from 'vite';

export interface InitialLoadOptions {
  /** Module the tiny entry imports dynamically: the application itself. */
  readonly appModule: string;
  /** Upper bound for the JavaScript every guest downloads before the menu shows, gzip-compressed. */
  readonly budgetKiB: number;
  /** Packages that must only ever load on demand (the 3D stack). */
  readonly lazyOnly: readonly string[];
}

/**
 * Guards the first load of the guest app, which starts from a QR code on a phone.
 *
 * The entry chunk only starts the menu request and then imports the application. This plugin
 * - adds modulepreload hints for the application chunks to index.html, so they download in parallel with the entry
 *   instead of one round trip later, and
 * - fails the build when the initial JavaScript exceeds its budget or contains a package meant to load lazily.
 */
export function initialLoad({ appModule, budgetKiB, lazyOnly }: InitialLoadOptions): Plugin {
  let base = '/';

  return {
    name: 'armenu:initial-load',
    apply: 'build',

    configResolved(config) {
      base = config.base;
    },

    transformIndexHtml: {
      order: 'post',
      handler(html, { bundle }) {
        if (bundle === undefined) {
          return html;
        }

        return appChunks(bundle, appModule)
          .filter((chunk) => !html.includes(chunk.fileName))
          .map((chunk) => ({
            tag: 'link',
            attrs: { rel: 'modulepreload', crossorigin: '', href: `${base}${chunk.fileName}` },
            injectTo: 'head' as const,
          }));
      },
    },

    generateBundle: {
      order: 'post',
      handler(_options, bundle) {
        const entry = chunksOf(bundle).find((chunk) => chunk.isEntry);
        if (entry === undefined) {
          this.error('The build has no entry chunk.');
        }

        const initial = new Set([...staticGraph(bundle, entry), ...appChunks(bundle, appModule)]);

        for (const chunk of initial) {
          const lazyModule = chunk.moduleIds.find((id) =>
            lazyOnly.some((name) => id.includes(`/node_modules/${name}/`)),
          );
          if (lazyModule !== undefined) {
            this.error(
              `${chunk.fileName} loads on startup but contains ${lazyModule}, which must stay lazy.`,
            );
          }
        }

        const gzipKiB =
          [...initial].reduce((total, chunk) => total + gzipSync(chunk.code).byteLength, 0) / 1024;
        this.info(`initial JavaScript: ${gzipKiB.toFixed(1)} KiB gzip (budget ${budgetKiB} KiB)`);
        if (gzipKiB > budgetKiB) {
          this.error(
            `Initial JavaScript is ${gzipKiB.toFixed(1)} KiB gzip, over its ${budgetKiB} KiB budget.`,
          );
        }
      },
    },
  };
}

function chunksOf(bundle: Rollup.OutputBundle): Rollup.OutputChunk[] {
  return Object.values(bundle).filter((output): output is Rollup.OutputChunk => output.type === 'chunk');
}

function appChunks(bundle: Rollup.OutputBundle, appModule: string): Rollup.OutputChunk[] {
  const app = chunksOf(bundle).find((chunk) => chunk.facadeModuleId?.endsWith(appModule));
  if (app === undefined) {
    throw new Error(`No chunk was emitted for ${appModule}.`);
  }

  return staticGraph(bundle, app);
}

/** A chunk and everything it imports statically: what the browser needs before running it. */
function staticGraph(bundle: Rollup.OutputBundle, root: Rollup.OutputChunk): Rollup.OutputChunk[] {
  const graph = new Map<string, Rollup.OutputChunk>();
  const visit = (chunk: Rollup.OutputChunk) => {
    if (graph.has(chunk.fileName)) {
      return;
    }

    graph.set(chunk.fileName, chunk);
    for (const fileName of chunk.imports) {
      const imported = bundle[fileName];
      if (imported?.type === 'chunk') {
        visit(imported);
      }
    }
  };

  visit(root);
  return [...graph.values()];
}
