import { readFile } from 'node:fs/promises';
import { createServer, type IncomingHttpHeaders, type Server } from 'node:http';
import type { AddressInfo } from 'node:net';

import { ModelRejectedError, ModelRenderer, processModel, type ProcessedModel } from '@armenu/model-pipeline';
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';

import type { ProcessorConfig } from '../src/config.ts';
import { parseJobRequest } from '../src/contract.ts';
import type { ProcessSource } from '../src/job.ts';
import { type Logger, silentLogger } from '../src/logger.ts';
import { createProcessorServer, type ProcessorServer } from '../src/server.ts';

const contract = (name: string) =>
  readFile(new URL(`../../../../contracts/asset-processor/${name}`, import.meta.url), 'utf8').then(
    (text) => JSON.parse(text) as Record<string, unknown>,
  );

const token = 'test-token-that-is-long-enough-for-the-rules';
const sourceBytes = new TextEncoder().encode('source model bytes');
const exampleResult = await contract('job-result.example.json');

interface StoredUpload {
  readonly path: string;
  readonly headers: IncomingHttpHeaders;
  readonly body: string;
}

/** Object storage as the processor sees it: presigned URLs that answer GET and PUT. */
class FakeStorage {
  readonly uploads: StoredUpload[] = [];
  source: Uint8Array = sourceBytes;
  uploadStatus = 200;
  readonly server: Server = createServer((request, response) => {
    const chunks: Buffer[] = [];
    request.on('data', (chunk: Buffer) => chunks.push(chunk));
    request.on('end', () => {
      if (request.method === 'GET') {
        response.writeHead(200, { 'Content-Length': this.source.byteLength }).end(this.source);
        return;
      }
      this.uploads.push({
        path: request.url ?? '',
        headers: request.headers,
        body: Buffer.concat(chunks).toString('utf8'),
      });
      response.writeHead(this.uploadStatus).end();
    });
  });

  get origin(): string {
    return `http://127.0.0.1:${(this.server.address() as AddressInfo).port}`;
  }
}

const storage = new FakeStorage();
let processor: ProcessorServer | undefined;

beforeAll(async () => {
  await new Promise<void>((resolve) => storage.server.listen(0, '127.0.0.1', resolve));
});

afterAll(async () => {
  await new Promise((resolve) => storage.server.close(resolve));
});

afterEach(async () => {
  await processor?.close();
  processor = undefined;
  storage.uploads.length = 0;
  storage.source = sourceBytes;
  storage.uploadStatus = 200;
});

function outputsOf(text: string): ProcessedModel {
  const bytes = (name: string) => new TextEncoder().encode(`${name} of ${text}`);
  return {
    model: bytes('model'),
    sceneViewerModel: bytes('sceneViewerModel'),
    appleModel: bytes('appleModel'),
    poster: bytes('poster'),
    report: exampleResult.report as ProcessedModel['report'],
  };
}

async function start(
  process: ProcessSource,
  overrides: Partial<ProcessorConfig> = {},
  logger: Logger = silentLogger,
): Promise<string> {
  processor = createProcessorServer(
    {
      port: 0,
      token,
      storageOrigins: new Set([storage.origin]),
      concurrency: 1,
      maxSourceBytes: 1024,
      browserChannel: undefined,
      ...overrides,
    },
    { logger, process },
  );
  const { server } = processor;
  await new Promise<void>((resolve) => server.listen(0, '127.0.0.1', resolve));
  return `http://127.0.0.1:${(server.address() as AddressInfo).port}`;
}

function jobFor(storageOrigin: string) {
  const upload = (name: string, contentType: string) => ({
    url: `${storageOrigin}/assets/${name}?X-Amz-Signature=signed`,
    headers: { 'Content-Type': contentType, 'Cache-Control': 'public, max-age=31536000, immutable' },
  });
  return {
    jobId: '01995a7e-3c1b-7d2e-9f40-6b8a1c2d3e4f',
    source: { url: `${storageOrigin}/uploads/source.glb?X-Amz-Signature=signed` },
    outputs: {
      model: upload('model.glb', 'model/gltf-binary'),
      sceneViewerModel: upload('model.scene-viewer.glb', 'model/gltf-binary'),
      appleModel: upload('model.usdz', 'model/vnd.usdz+zip'),
      poster: upload('model.webp', 'image/webp'),
    },
  };
}

function post(baseUrl: string, body: unknown, authorization = `Bearer ${token}`) {
  return fetch(`${baseUrl}/v1/jobs`, {
    method: 'POST',
    headers: { Authorization: authorization, 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
}

/** The same keys at every level, whatever the values: a response keeps the documented shape. */
function shapeOf(value: unknown): unknown {
  if (Array.isArray(value)) {
    return 'array';
  }
  if (typeof value === 'object' && value !== null) {
    return Object.fromEntries(Object.entries(value).map(([key, entry]) => [key, shapeOf(entry)]));
  }
  return typeof value;
}

describe('the contract examples', () => {
  it('describe a request the processor accepts', async () => {
    const request = parseJobRequest(
      await contract('job-request.example.json'),
      new Set(['https://storage.example.com']),
    );

    expect(request.outputs.appleModel.headers['Content-Type']).toBe('model/vnd.usdz+zip');
  });
});

describe('POST /v1/jobs', () => {
  it('processes the source and uploads every output with its signed headers', async () => {
    const baseUrl = await start((source) => Promise.resolve(outputsOf(new TextDecoder().decode(source))));

    const response = await post(baseUrl, jobFor(storage.origin));

    expect(response.status).toBe(200);
    expect(await response.json()).toEqual(exampleResult);
    expect(
      storage.uploads.map(({ path, headers, body }) => [
        path,
        headers['content-type'],
        headers['cache-control'],
        body,
      ]),
    ).toEqual(
      expect.arrayContaining([
        [
          '/assets/model.glb?X-Amz-Signature=signed',
          'model/gltf-binary',
          'public, max-age=31536000, immutable',
          'model of source model bytes',
        ],
        [
          '/assets/model.scene-viewer.glb?X-Amz-Signature=signed',
          'model/gltf-binary',
          'public, max-age=31536000, immutable',
          'sceneViewerModel of source model bytes',
        ],
        [
          '/assets/model.usdz?X-Amz-Signature=signed',
          'model/vnd.usdz+zip',
          'public, max-age=31536000, immutable',
          'appleModel of source model bytes',
        ],
        [
          '/assets/model.webp?X-Amz-Signature=signed',
          'image/webp',
          'public, max-age=31536000, immutable',
          'poster of source model bytes',
        ],
      ]),
    );
  });

  it("logs the job under the API's trace", async () => {
    const lines: [string, Record<string, unknown> | undefined][] = [];
    const record = (message: string, fields?: Record<string, unknown>) => {
      lines.push([message, fields]);
    };
    const baseUrl = await start(
      (source) => Promise.resolve(outputsOf(new TextDecoder().decode(source))),
      {},
      { info: record, error: record },
    );

    await fetch(`${baseUrl}/v1/jobs`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
        traceparent: '00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01',
      },
      body: JSON.stringify(jobFor(storage.origin)),
    });

    expect(lines).toEqual([
      [
        'Job processed',
        expect.objectContaining({
          traceId: '4bf92f3577b34da6a3ce929d0e0e4736',
          parentSpanId: '00f067aa0ba902b7',
        }),
      ],
    ]);
  });

  it('refuses callers without the token', async () => {
    const baseUrl = await start(() => Promise.reject(new Error('must not run')));

    const response = await post(baseUrl, jobFor(storage.origin), 'Bearer wrong-token');

    expect(response.status).toBe(401);
    expect(await response.json()).toMatchObject({ code: 'processor.unauthorized' });
  });

  it('never fetches a URL outside object storage', async () => {
    const baseUrl = await start(() => Promise.reject(new Error('must not run')));
    const job = jobFor(storage.origin);

    const response = await post(baseUrl, {
      ...job,
      source: { url: 'http://169.254.169.254/latest/meta-data/' },
    });

    expect(response.status).toBe(400);
    expect(await response.json()).toMatchObject({ code: 'processor.invalid_request' });
  });

  it('reports a model that cannot be published as a final rejection, in the documented shape', async () => {
    const baseUrl = await start(() =>
      Promise.reject(new ModelRejectedError('model.texture_unsupported', "Texture 'crust' is image/ktx2.")),
    );

    const response = await post(baseUrl, jobFor(storage.origin));
    const body: unknown = await response.json();

    expect(response.status).toBe(422);
    expect(body).toEqual(await contract('job-rejected.example.json'));
    expect(storage.uploads).toEqual([]);
  });

  it('refuses a source over the size limit before processing it', async () => {
    const baseUrl = await start(() => Promise.reject(new Error('must not run')), { maxSourceBytes: 8 });

    const response = await post(baseUrl, jobFor(storage.origin));

    expect(response.status).toBe(422);
    expect(await response.json()).toMatchObject({ code: 'model.too_large' });
  });

  it('answers a storage failure with 502, which the API retries', async () => {
    storage.uploadStatus = 403;
    const baseUrl = await start((source) => Promise.resolve(outputsOf(new TextDecoder().decode(source))));

    const response = await post(baseUrl, jobFor(storage.origin));

    expect(response.status).toBe(502);
    expect(await response.json()).toMatchObject({ code: 'processor.storage_failed' });
  });

  it('turns jobs beyond its concurrency away at once', async () => {
    const { promise: released, resolve: release } = Promise.withResolvers<undefined>();
    let started = false;
    const baseUrl = await start(async (source) => {
      started = true;
      await released;
      return outputsOf(new TextDecoder().decode(source));
    });

    const first = post(baseUrl, jobFor(storage.origin));
    await expect.poll(() => started).toBe(true);
    const second = await post(baseUrl, jobFor(storage.origin));
    release(undefined);

    expect(second.status).toBe(503);
    expect(second.headers.get('retry-after')).toBe('5');
    expect((await first).status).toBe(200);
  });
});

describe('with the real pipeline', () => {
  let renderer: ModelRenderer;

  beforeAll(async () => {
    renderer = await ModelRenderer.launch({ channel: 'chrome' });
  });

  afterAll(async () => {
    await renderer.close();
  });

  it('turns a GLB into files guests can open', { timeout: 60_000 }, async () => {
    storage.source = await readFile(
      new URL('../../../../assets/demo/models/smash-burger.scene-viewer.glb', import.meta.url),
    );
    const baseUrl = await start((source) => processModel(source, renderer), { maxSourceBytes: 1024 * 1024 });

    const response = await post(baseUrl, jobFor(storage.origin));

    expect(response.status).toBe(200);
    expect(shapeOf(await response.json())).toEqual(shapeOf(exampleResult));
    expect(storage.uploads).toHaveLength(4);
  });
});
