import { Logger } from '@gltf-transform/core';

import { gltfIO } from './gltf-io.ts';
import { defaultLimits, forSceneViewer, forWeb, type PipelineLimits, prepare } from './optimize.ts';
import { ModelRejectedError } from './rejection.ts';
import type { ModelRenderer } from './render.ts';
import { dimensionsOf, type ModelDimensions, type ModelStats, statsOf } from './stats.ts';

/** Something worth a look, which did not stop the model from being published. */
export type ModelWarning =
  /** Triangles were reduced to the budget. */
  | 'model.simplified'
  /** Textures were larger than phones need and were downscaled. */
  | 'model.textures_downscaled'
  /** Over 1.5 m or under 2 cm: usually a model exported in the wrong unit. AR shows dishes at their real size. */
  | 'model.unusual_size'
  /** Every material is a draw call; food rarely needs more than a few. */
  | 'model.many_materials'
  /** The in-page model is still over 5 MiB, slow to open on a mobile network. */
  | 'model.large_download'
  /** Over Google's 10 MB guidance for Scene Viewer. */
  | 'model.scene_viewer_too_large';

export interface ModelReport {
  readonly source: ModelStats;
  readonly optimized: ModelStats;
  readonly files: {
    readonly source: number;
    readonly model: number;
    readonly sceneViewerModel: number;
    readonly appleModel: number;
    readonly poster: number;
  };
  readonly dimensions: ModelDimensions;
  readonly warnings: readonly ModelWarning[];
}

export interface ProcessedModel {
  /** Meshopt + WebP GLB for the in-page viewer and WebXR. */
  readonly model: Uint8Array;
  /** Plain GLB for Android Scene Viewer. */
  readonly sceneViewerModel: Uint8Array;
  /** USDZ for iOS AR Quick Look. */
  readonly appleModel: Uint8Array;
  /** WebP poster. */
  readonly poster: Uint8Array;
  readonly report: ModelReport;
}

const mebibyte = 1024 * 1024;

/**
 * Turns one uploaded GLB into every file a guest's device needs, each in the format that device decodes fastest.
 *
 * @throws {ModelRejectedError} When the file cannot become a menu model. Anything else is an infrastructure fault.
 */
export async function processModel(
  source: Uint8Array,
  renderer: ModelRenderer,
  limits: PipelineLimits = defaultLimits,
): Promise<ProcessedModel> {
  if (source.byteLength > limits.maxSourceBytes) {
    throw new ModelRejectedError('model.too_large', `The source exceeds ${limits.maxSourceBytes} bytes.`);
  }

  const io = await gltfIO();

  let document;
  try {
    document = await io.readBinary(source);
  } catch (error) {
    throw new ModelRejectedError('model.unreadable', 'The file is not a readable glTF 2.0 binary.', {
      cause: error,
    });
  }

  // Transforms log every step at info level; a service only needs to hear about problems.
  document.setLogger(new Logger(Logger.Verbosity.WARN));

  const sourceStats = statsOf(document);
  const prepared = await prepare(document, limits);
  const sceneViewer = await forSceneViewer(prepared.document, limits);
  const web = await forWeb(prepared.document, limits);

  const model = await io.writeBinary(web.document);
  const sceneViewerModel = await io.writeBinary(sceneViewer);
  const { poster, appleModel } = await renderer.render(model);

  const optimized = statsOf(web.document);
  const dimensions = dimensionsOf(web.document);
  const longestSide = Math.max(dimensions.width, dimensions.height, dimensions.depth);

  const warnings: ModelWarning[] = [];
  const warn = (condition: boolean, warning: ModelWarning) => {
    if (condition) {
      warnings.push(warning);
    }
  };
  warn(prepared.simplified, 'model.simplified');
  warn(web.downscaled, 'model.textures_downscaled');
  warn(longestSide > 1.5 || longestSide < 0.02, 'model.unusual_size');
  warn(optimized.materials > 10, 'model.many_materials');
  warn(model.byteLength > 5 * mebibyte, 'model.large_download');
  warn(sceneViewerModel.byteLength > 10 * mebibyte, 'model.scene_viewer_too_large');

  return {
    model,
    sceneViewerModel,
    appleModel,
    poster,
    report: {
      source: sourceStats,
      optimized,
      files: {
        source: source.byteLength,
        model: model.byteLength,
        sceneViewerModel: sceneViewerModel.byteLength,
        appleModel: appleModel.byteLength,
        poster: poster.byteLength,
      },
      dimensions,
      warnings,
    },
  };
}
