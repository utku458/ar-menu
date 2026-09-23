import { queryOptions } from '@tanstack/react-query';

import { unwrap } from '../api/result.ts';
import type { Workspace } from '../auth/workspaces.ts';

/** Every business on the platform, read with the administrator's own session. */
export function businessesQuery(platform: Workspace) {
  return queryOptions({
    queryKey: ['platform', 'businesses'],
    queryFn: async ({ signal }) => unwrap(await platform.api.GET('/api/v1/platform/businesses', { signal })),
    staleTime: 60_000,
  });
}
