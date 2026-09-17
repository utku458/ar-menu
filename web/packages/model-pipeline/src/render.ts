import { readFile } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { dirname, join } from 'node:path';

import { posterSize, viewerPreset } from '@armenu/ar-viewer/preset';
import { type Browser, chromium, type LaunchOptions, type Route } from 'playwright-core';

import { ModelRejectedError } from './rejection.ts';

export interface RenderedModel {
  /** WebP still of the first frame guests see. */
  readonly poster: Uint8Array;
  /** USDZ for iOS AR Quick Look. */
  readonly appleModel: Uint8Array;
}

export interface RendererOptions {
  /** Browser channel, such as `chrome` for an installed Google Chrome. Default: Playwright's own Chromium. */
  readonly channel?: string | undefined;
  /** Upper bound for loading and rendering one model. */
  readonly timeoutMs?: number;
}

/** The iPhone downloads a USDZ completely before AR opens, and it stores textures as PNG: 1024 px keeps it light. */
export const appleModelMaxTextureSize = 1024;

const origin = 'https://renderer.armenu.invalid';
const require = createRequire(import.meta.url);

interface RenderableViewer extends HTMLElement {
  readonly loaded: boolean;
  toBlob(options: { mimeType: string; qualityArgument: number; idealAspect: boolean }): Promise<Blob>;
  prepareUSDZ(): Promise<string>;
}

/**
 * Loads a model into <model-viewer>, the viewer guests use, in headless Chrome: a model that renders here renders on
 * guests' phones. From the loaded scene it captures the poster and exports the USDZ, exactly as <model-viewer> would
 * convert the model on an iPhone.
 *
 * Pages never touch the network: the viewer, the decoder and the model are served from memory, and any other request
 * is refused. One browser is shared; every model gets its own isolated context.
 */
export class ModelRenderer {
  readonly #browser: Browser;
  readonly #viewerScript: Buffer;
  readonly #meshoptDecoderScript: Buffer;
  readonly #timeoutMs: number;

  private constructor(
    browser: Browser,
    viewerScript: Buffer,
    meshoptDecoderScript: Buffer,
    timeoutMs: number,
  ) {
    this.#browser = browser;
    this.#viewerScript = viewerScript;
    this.#meshoptDecoderScript = meshoptDecoderScript;
    this.#timeoutMs = timeoutMs;
  }

  static async launch({ channel, timeoutMs = 60_000 }: RendererOptions = {}): Promise<ModelRenderer> {
    const launchOptions: LaunchOptions = {
      // WebGL without a GPU (containers) uses SwiftShader, which Chrome only enables on request.
      args: ['--enable-unsafe-swiftshader'],
      ...(channel === undefined ? {} : { channel }),
    };

    const [browser, viewerScript, meshoptDecoderScript] = await Promise.all([
      chromium.launch(launchOptions),
      readFile(require.resolve('@google/model-viewer/dist/model-viewer.min.js')),
      // <model-viewer> enables its bundled Meshopt decoder only after loading a decoder script: serve meshoptimizer's own.
      readFile(join(dirname(require.resolve('meshoptimizer')), 'meshopt_decoder.cjs')),
    ]);

    return new ModelRenderer(browser, viewerScript, meshoptDecoderScript, timeoutMs);
  }

  get isConnected(): boolean {
    return this.#browser.isConnected();
  }

  async render(model: Uint8Array): Promise<RenderedModel> {
    const context = await this.#browser.newContext({
      viewport: { width: posterSize, height: posterSize },
      deviceScaleFactor: 1,
      serviceWorkers: 'block',
    });
    context.setDefaultTimeout(this.#timeoutMs);

    try {
      await context.route('**/*', (route) => this.#serve(route, model));
      const page = await context.newPage();
      await page.goto(`${origin}/`);

      const result = await page.evaluate(capture, { timeoutMs: this.#timeoutMs });
      if ('error' in result) {
        throw new ModelRejectedError(
          'model.render_failed',
          `The viewer could not show the model: ${result.error}`,
        );
      }

      return {
        poster: Buffer.from(result.poster, 'base64'),
        appleModel: Buffer.from(result.appleModel, 'base64'),
      };
    } finally {
      await context.close();
    }
  }

  async close(): Promise<void> {
    await this.#browser.close();
  }

  #serve(route: Route, model: Uint8Array): Promise<void> {
    const url = new URL(route.request().url());
    if (url.origin !== origin) {
      return route.abort('blockedbyclient');
    }

    switch (url.pathname) {
      case '/':
        return route.fulfill({ body: page(), contentType: 'text/html' });
      case '/model-viewer.js':
        return route.fulfill({ body: this.#viewerScript, contentType: 'text/javascript' });
      case '/meshopt_decoder.js':
        return route.fulfill({ body: this.#meshoptDecoderScript, contentType: 'text/javascript' });
      case '/model.glb':
        return route.fulfill({ body: Buffer.from(model), contentType: 'model/gltf-binary' });
      default:
        return route.abort('blockedbyclient');
    }
  }
}

function page(): string {
  return `<!doctype html>
<meta charset="utf-8">
<style>html, body { margin: 0; background: transparent; } model-viewer { width: 100vw; height: 100vh; }</style>
<script>self.ModelViewerElement = { meshoptDecoderLocation: '/meshopt_decoder.js' };</script>
<script type="module" src="/model-viewer.js"></script>
<model-viewer
  src="/model.glb"
  camera-orbit="${viewerPreset.cameraOrbit}"
  environment-image="${viewerPreset.environmentImage}"
  tone-mapping="${viewerPreset.toneMapping}"
  exposure="${viewerPreset.exposure}"
  shadow-intensity="${viewerPreset.shadowIntensity}"
  shadow-softness="${viewerPreset.shadowSoftness}"
  ar-usdz-max-texture-size="${appleModelMaxTextureSize}"
  interaction-prompt="none"
></model-viewer>`;
}

// Runs inside the page.
async function capture({
  timeoutMs,
}: {
  timeoutMs: number;
}): Promise<{ poster: string; appleModel: string } | { error: string }> {
  const toBase64 = async (blob: Blob) => {
    const bytes = new Uint8Array(await blob.arrayBuffer());
    let binary = '';
    for (let offset = 0; offset < bytes.length; offset += 0x8000) {
      binary += String.fromCharCode(...bytes.subarray(offset, offset + 0x8000));
    }
    return btoa(binary);
  };

  try {
    await customElements.whenDefined('model-viewer');
    const viewer = document.querySelector<RenderableViewer>('model-viewer');
    if (viewer === null) {
      return { error: 'the page has no <model-viewer>' };
    }

    if (!viewer.loaded) {
      await new Promise((resolve, reject) => {
        const timer = setTimeout(() => {
          reject(new Error('loading timed out'));
        }, timeoutMs);
        viewer.addEventListener(
          'load',
          () => {
            clearTimeout(timer);
            resolve(undefined);
          },
          { once: true },
        );
        viewer.addEventListener(
          'error',
          (event) => {
            clearTimeout(timer);
            const detail = (event as unknown as CustomEvent<{ sourceError?: unknown }>).detail.sourceError;
            reject(new Error(detail instanceof Error ? detail.message : 'the model failed to load'));
          },
          { once: true },
        );
      });
    }

    // Let the shadow and the environment settle into a rendered frame.
    for (let frame = 0; frame < 3; frame++) {
      await new Promise(requestAnimationFrame);
    }

    const poster = await viewer.toBlob({ mimeType: 'image/webp', qualityArgument: 0.88, idealAspect: false });
    const appleModelUrl = await viewer.prepareUSDZ();
    const appleModel = await (await fetch(appleModelUrl)).blob();
    URL.revokeObjectURL(appleModelUrl);

    return { poster: await toBase64(poster), appleModel: await toBase64(appleModel) };
  } catch (error) {
    return { error: error instanceof Error ? error.message : String(error) };
  }
}
