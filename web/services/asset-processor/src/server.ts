import { timingSafeEqual } from 'node:crypto';
import { createServer, type IncomingMessage, type Server, type ServerResponse } from 'node:http';

import { ModelRejectedError } from '@armenu/model-pipeline';

import type { ProcessorConfig } from './config.ts';
import { InvalidJobRequestError, parseJobRequest, type Problem } from './contract.ts';
import { type JobDependencies, runJob, StorageTransferError } from './job.ts';
import type { Logger } from './logger.ts';
import { traceContextOf } from './trace-context.ts';

export interface ServerDependencies extends Omit<JobDependencies, 'maxSourceBytes'> {
  readonly logger: Logger;
}

export interface ProcessorServer {
  readonly server: Server;
  /** Stops accepting requests and waits for running jobs to finish. */
  readonly close: () => Promise<void>;
}

const maxRequestBodyBytes = 64 * 1024;

/**
 * An internal HTTP service with one operation: POST /v1/jobs processes a model synchronously and answers with its
 * report. Jobs beyond the concurrency limit are refused with 503 at once rather than queued, so the API's job queue
 * stays the single place where work waits and retries.
 */
export function createProcessorServer(
  config: ProcessorConfig,
  dependencies: ServerDependencies,
): ProcessorServer {
  const { logger } = dependencies;
  const expectedAuthorization = Buffer.from(`Bearer ${config.token}`);
  const running = new Set<Promise<unknown>>();

  const server = createServer((request, response) => {
    const handled = handle(request, response).catch((error: unknown) => {
      logger.error('Unhandled request failure', { error: describe(error) });
      if (!response.headersSent) {
        sendProblem(response, problem(500, 'processor.failed', 'The processor failed unexpectedly.'));
      }
    });
    running.add(handled);
    void handled.finally(() => running.delete(handled));
  });

  let activeJobs = 0;

  async function handle(request: IncomingMessage, response: ServerResponse): Promise<void> {
    const { pathname } = new URL(request.url ?? '/', 'http://processor');

    // The browser is launched before the server listens: listening means ready.
    if (request.method === 'GET' && (pathname === '/health/live' || pathname === '/health/ready')) {
      sendJson(response, 200, { status: 'healthy' });
      return;
    }
    if (pathname !== '/v1/jobs') {
      sendProblem(response, problem(404, 'processor.not_found', 'There is no such endpoint.'));
      return;
    }
    if (request.method !== 'POST') {
      response.setHeader('Allow', 'POST');
      sendProblem(response, problem(405, 'processor.method_not_allowed', 'Jobs are created with POST.'));
      return;
    }
    if (!isAuthorized(request.headers.authorization)) {
      response.setHeader('WWW-Authenticate', 'Bearer');
      sendProblem(response, problem(401, 'processor.unauthorized', 'A valid bearer token is required.'));
      return;
    }
    if (activeJobs >= config.concurrency) {
      response.setHeader('Retry-After', '5');
      sendProblem(response, problem(503, 'processor.busy', 'All workers are busy; retry later.'));
      return;
    }

    activeJobs++;
    const started = performance.now();
    const trace = traceContextOf(request.headers.traceparent);
    let jobId: string | undefined;
    try {
      const job = parseJobRequest(await readJson(request), config.storageOrigins);
      jobId = job.jobId;

      // A disconnected API abandons the job: stop transferring rather than finishing for nobody.
      const abort = new AbortController();
      response.on('close', () => {
        if (!response.writableFinished) {
          abort.abort();
        }
      });

      const result = await runJob(
        job,
        { ...dependencies, maxSourceBytes: config.maxSourceBytes },
        abort.signal,
      );
      logger.info('Job processed', {
        jobId,
        ...trace,
        durationMs: Math.round(performance.now() - started),
        report: result.report,
      });
      sendJson(response, 200, result);
    } catch (error) {
      const answer = problemFor(error);
      const level = answer.status >= 500 ? 'error' : 'info';
      logger[level]('Job not processed', {
        jobId,
        ...trace,
        status: answer.status,
        code: answer.code,
        error: describe(error),
      });
      sendProblem(response, answer);
    } finally {
      activeJobs--;
    }
  }

  function isAuthorized(header: string | undefined): boolean {
    const actual = Buffer.from(header ?? '');
    return actual.length === expectedAuthorization.length && timingSafeEqual(actual, expectedAuthorization);
  }

  return {
    server,
    close: async () => {
      await new Promise<void>((resolve) =>
        server.close(() => {
          resolve();
        }),
      );
      await Promise.allSettled(running);
    },
  };
}

function problemFor(error: unknown): Problem {
  if (error instanceof InvalidJobRequestError) {
    return problem(400, 'processor.invalid_request', error.message);
  }
  if (error instanceof ModelRejectedError) {
    return {
      type: 'https://armenu.app/problems/model-rejected',
      title: 'The model cannot be published.',
      status: 422,
      code: error.code,
      detail: error.message,
    };
  }
  if (error instanceof StorageTransferError) {
    return problem(502, 'processor.storage_failed', error.message);
  }
  return problem(500, 'processor.failed', 'The processor failed unexpectedly.');
}

function problem(status: number, code: string, detail: string): Problem {
  return { type: 'about:blank', title: detail, status, code, detail };
}

async function readJson(request: IncomingMessage): Promise<unknown> {
  if (!request.headers['content-type']?.startsWith('application/json')) {
    throw new InvalidJobRequestError('The body must be application/json.');
  }

  const chunks: Buffer[] = [];
  let length = 0;
  for await (const chunk of request as AsyncIterable<Buffer>) {
    length += chunk.byteLength;
    if (length > maxRequestBodyBytes) {
      throw new InvalidJobRequestError('The request body is too large.');
    }
    chunks.push(chunk);
  }

  try {
    return JSON.parse(Buffer.concat(chunks).toString('utf8')) as unknown;
  } catch {
    throw new InvalidJobRequestError('The body is not valid JSON.');
  }
}

function sendJson(response: ServerResponse, status: number, body: unknown): void {
  response.writeHead(status, { 'Content-Type': 'application/json' }).end(JSON.stringify(body));
}

function sendProblem(response: ServerResponse, body: Problem): void {
  response.writeHead(body.status, { 'Content-Type': 'application/problem+json' }).end(JSON.stringify(body));
}

function describe(error: unknown): string {
  return error instanceof Error ? `${error.name}: ${error.message}` : String(error);
}
