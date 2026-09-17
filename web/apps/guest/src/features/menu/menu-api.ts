import { ApiError, createApiClient, type PublicMenuResponse } from '@armenu/api-client';

import { env } from '../../env.ts';
import type { MenuRequest } from './menu-request.ts';

const api = createApiClient(env.apiBaseUrl);

export async function fetchPublicMenu(
  request: MenuRequest,
  signal?: AbortSignal,
): Promise<PublicMenuResponse> {
  const { data, error, response } = await api.GET('/api/v1/menus/{tenant}', {
    params: {
      path: { tenant: request.tenant },
      query: request.lang === undefined ? {} : { lang: request.lang },
    },
    signal: signal ?? null,
  });

  if (data !== undefined) {
    return data;
  }

  throw ApiError.fromResponse(response, error);
}
