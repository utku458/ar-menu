import { ApiError } from '@armenu/api-client';

interface FetchResult<T> {
  readonly data?: T;
  readonly error?: unknown;
  readonly response: Response;
}

/** The response body of a successful call; any other answer becomes an {@link ApiError}. */
export function unwrap<T>(result: FetchResult<T>): T {
  if (result.response.ok && result.data !== undefined) {
    return result.data;
  }

  throw ApiError.fromResponse(result.response, result.error);
}

/** For calls answered with 204 No Content. */
export function ensureOk(result: FetchResult<unknown>): void {
  if (!result.response.ok) {
    throw ApiError.fromResponse(result.response, result.error);
  }
}
