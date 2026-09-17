import type { ProblemDetails } from './schema.gen.ts';

/** A non-successful API response, carrying the RFC 9457 problem details the API sent (if any). */
export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails | undefined;
  /** From the `Retry-After` header of 429 and 503 responses. */
  readonly retryAfterSeconds: number | undefined;

  constructor(status: number, problem?: ProblemDetails, retryAfterSeconds?: number) {
    super(problem?.detail ?? problem?.title ?? `The API answered with status ${status}.`);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
    this.retryAfterSeconds = retryAfterSeconds;
  }

  static fromResponse(response: Response, body: unknown): ApiError {
    return new ApiError(
      response.status,
      isProblemDetails(body) ? body : undefined,
      parseRetryAfter(response.headers.get('Retry-After')),
    );
  }

  /** Stable, machine-readable error code, e.g. `tenant.not_found`. */
  get code(): string | undefined {
    return this.problem?.code;
  }
}

function isProblemDetails(body: unknown): body is ProblemDetails {
  return typeof body === 'object' && body !== null && !Array.isArray(body);
}

// Only the delta-seconds form is used by the API; HTTP dates are ignored rather than misread.
function parseRetryAfter(header: string | null): number | undefined {
  if (header === null || !/^\d+$/.test(header.trim())) {
    return undefined;
  }

  return Number(header.trim());
}
