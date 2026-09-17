import { ModelRejectedError, type ProcessedModel } from '@armenu/model-pipeline';

import { type JobRequest, type JobResult, outputNames } from './contract.ts';

/** Turns source bytes into the outputs. Production uses the model pipeline; tests can substitute it. */
export type ProcessSource = (source: Uint8Array) => Promise<ProcessedModel>;

export interface JobDependencies {
  readonly process: ProcessSource;
  readonly maxSourceBytes: number;
  readonly fetch?: typeof fetch;
}

/** Storage refused or failed a transfer. Transient from the API's point of view: the job is retried. */
export class StorageTransferError extends Error {
  override name = 'StorageTransferError';
}

/** Downloads the source, processes it and uploads every output to its presigned URL. */
export async function runJob(
  request: JobRequest,
  dependencies: JobDependencies,
  signal: AbortSignal,
): Promise<JobResult> {
  const { process, maxSourceBytes, fetch: fetchImpl = fetch } = dependencies;

  const source = await download(request.source.url, maxSourceBytes, fetchImpl, signal);
  const processed = await process(source);

  await Promise.all(
    outputNames.map(async (name) => {
      const { url, headers } = request.outputs[name];
      // The pipeline's buffers are never shared memory; the DOM typings cannot know that.
      const body = processed[name] as Uint8Array<ArrayBuffer>;
      const response = await fetchImpl(url, { method: 'PUT', headers, body, signal });
      if (!response.ok) {
        throw new StorageTransferError(`Storage refused the ${name} upload with HTTP ${response.status}.`);
      }
    }),
  );

  return { report: processed.report };
}

async function download(
  url: URL,
  maxBytes: number,
  fetchImpl: typeof fetch,
  signal: AbortSignal,
): Promise<Uint8Array> {
  const response = await fetchImpl(url, { signal });
  if (!response.ok || response.body === null) {
    throw new StorageTransferError(`Storage answered the source download with HTTP ${response.status}.`);
  }

  const declared = Number(response.headers.get('content-length'));
  if (declared > maxBytes) {
    throw tooLarge(maxBytes);
  }

  // The declared length can be missing: count while streaming, so memory stays bounded either way.
  const chunks: Uint8Array[] = [];
  let received = 0;
  for await (const chunk of response.body) {
    received += chunk.byteLength;
    if (received > maxBytes) {
      throw tooLarge(maxBytes);
    }
    chunks.push(chunk);
  }

  return Buffer.concat(chunks);
}

function tooLarge(maxBytes: number): ModelRejectedError {
  return new ModelRejectedError('model.too_large', `The source exceeds ${maxBytes} bytes.`);
}
