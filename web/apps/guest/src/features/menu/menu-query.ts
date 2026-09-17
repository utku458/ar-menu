import { ApiError } from '@armenu/api-client';
import { queryOptions } from '@tanstack/react-query';

import { fetchPublicMenu } from './menu-api.ts';
import { takePreloadedMenu } from './menu-preload.ts';
import type { MenuRequest } from './menu-request.ts';

const maxRetries = 2;

export function publicMenuQuery(request: MenuRequest) {
  return queryOptions({
    queryKey: ['public-menu', request],
    queryFn: ({ signal }) => takePreloadedMenu(request) ?? fetchPublicMenu(request, signal),
    // Matches the API's Cache-Control max-age: a sold-out dish shows up within half a minute.
    staleTime: 30_000,
    // Flaky mobile networks and server errors are retried; answers about the request itself (404, 429) are not.
    retry: (failureCount, error) =>
      failureCount < maxRetries && !(error instanceof ApiError && error.status >= 400 && error.status < 500),
  });
}
