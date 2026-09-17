import createClient, { type Client } from 'openapi-fetch';

import type { paths } from './schema.gen.ts';

export type * from './schema.gen.ts';
export { ApiError } from './api-error.ts';

/** Typed HTTP client: paths, parameters and response bodies all come from the OpenAPI contract. */
export type ApiClient = Client<paths>;

export function createApiClient(baseUrl: string): ApiClient {
  return createClient<paths>({ baseUrl });
}
