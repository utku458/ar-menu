import type { ModelReport } from '@armenu/model-pipeline';

/**
 * The job the API hands over (contracts/asset-processor/job-request.example.json). Storage is only reachable through
 * the presigned URLs in it: the processor holds no storage credentials and can touch nothing else.
 */
export interface JobRequest {
  readonly jobId: string;
  readonly source: { readonly url: URL };
  readonly outputs: Readonly<Record<OutputName, PresignedUpload>>;
}

export interface PresignedUpload {
  readonly url: URL;
  /** Signed together with the URL: sent exactly as given. */
  readonly headers: Readonly<Record<string, string>>;
}

export const outputNames = ['model', 'sceneViewerModel', 'appleModel', 'poster'] as const;
export type OutputName = (typeof outputNames)[number];

/** contracts/asset-processor/job-result.example.json */
export interface JobResult {
  readonly report: ModelReport;
}

/** contracts/asset-processor/job-rejected.example.json: RFC 9457 problem details with a stable code. */
export interface Problem {
  readonly type: string;
  readonly title: string;
  readonly status: number;
  readonly code: string;
  readonly detail: string;
}

export class InvalidJobRequestError extends Error {
  override name = 'InvalidJobRequestError';
}

const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/** Validates an untrusted request body. URLs must point at an allowed storage origin. */
export function parseJobRequest(body: unknown, storageOrigins: ReadonlySet<string>): JobRequest {
  const object = (value: unknown, path: string): Record<string, unknown> => {
    if (typeof value !== 'object' || value === null || Array.isArray(value)) {
      throw new InvalidJobRequestError(`${path} must be an object.`);
    }
    return value as Record<string, unknown>;
  };

  const storageUrl = (value: unknown, path: string): URL => {
    if (typeof value !== 'string' || !URL.canParse(value)) {
      throw new InvalidJobRequestError(`${path} must be a URL.`);
    }
    const url = new URL(value);
    if (!storageOrigins.has(url.origin)) {
      throw new InvalidJobRequestError(`${path} does not point at an allowed storage origin.`);
    }
    return url;
  };

  const root = object(body, 'The request');
  if (typeof root.jobId !== 'string' || !uuid.test(root.jobId)) {
    throw new InvalidJobRequestError('jobId must be a UUID.');
  }

  const outputs = object(root.outputs, 'outputs');
  const parsedOutputs = Object.fromEntries(
    outputNames.map((name) => {
      const output = object(outputs[name], `outputs.${name}`);
      const headers = object(output.headers, `outputs.${name}.headers`);
      if (Object.values(headers).some((value) => typeof value !== 'string')) {
        throw new InvalidJobRequestError(`outputs.${name}.headers must map names to strings.`);
      }
      return [
        name,
        { url: storageUrl(output.url, `outputs.${name}.url`), headers: headers as Record<string, string> },
      ];
    }),
  ) as Record<OutputName, PresignedUpload>;

  return {
    jobId: root.jobId,
    source: { url: storageUrl(object(root.source, 'source').url, 'source.url') },
    outputs: parsedOutputs,
  };
}
