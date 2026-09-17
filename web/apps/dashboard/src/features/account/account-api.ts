import { queryOptions } from '@tanstack/react-query';

import { unwrap } from '../../api/result.ts';
import type { Workspace } from '../../auth/workspaces.ts';

/** What deleting the account would do; asked again whenever the account page opens, since teams change. */
export function accountDeletionQuery(workspace: Workspace) {
  return queryOptions({
    queryKey: ['account-deletion', workspace.slug],
    queryFn: async ({ signal }) => unwrap(await workspace.api.GET('/api/v1/me/deletion', { signal })),
    staleTime: 0,
  });
}
