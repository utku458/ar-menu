/**
 * Why a model cannot be published. Rejections are final: processing the same file again gives the same answer, so the
 * API reports them to the business instead of retrying.
 */
export type ModelRejection =
  /** Larger than the pipeline accepts. */
  | 'model.too_large'
  /** Not a glTF 2.0 binary the reader understands. */
  | 'model.unreadable'
  /** No triangles to show. */
  | 'model.empty'
  /** Far beyond what a phone can show; simplifying it would take minutes and still lose the dish. */
  | 'model.too_complex'
  /** A texture the pipeline cannot decode, such as KTX2, which would need a transcoder on every device. */
  | 'model.texture_unsupported'
  /** The optimized model did not load in the viewer guests use. */
  | 'model.render_failed';

export class ModelRejectedError extends Error {
  readonly code: ModelRejection;

  constructor(code: ModelRejection, detail: string, options?: ErrorOptions) {
    super(detail, options);
    this.name = 'ModelRejectedError';
    this.code = code;
  }
}
