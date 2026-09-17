import { infiniteQueryOptions } from '@tanstack/react-query';

import { unwrap } from '../../api/result.ts';
import type { Workspace } from '../../auth/workspaces.ts';

export const historyPageSize = 50;

/** The business's history, newest first; each page continues before the last entry of the previous one. */
export function historyQuery(workspace: Workspace) {
  return infiniteQueryOptions({
    queryKey: ['history', workspace.slug],
    queryFn: async ({ pageParam, signal }) =>
      unwrap(
        await workspace.api.GET('/api/v1/manage/history', {
          params: {
            query: { limit: historyPageSize, ...(pageParam === undefined ? {} : { before: pageParam }) },
          },
          signal,
        }),
      ),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (page) => page.nextCursor ?? undefined,
    staleTime: 0,
  });
}
