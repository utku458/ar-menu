import { queryOptions, useSuspenseQuery } from '@tanstack/react-query';
import { getRouteApi } from '@tanstack/react-router';

import { unwrap } from '../api/result.ts';
import type { Workspace } from '../auth/workspaces.ts';

const workspaceRoute = getRouteApi('/$workspace');

export function meQuery(workspace: Workspace) {
  return queryOptions({
    // The API client in the query function is the business's singleton; the slug identifies it.
    queryKey: ['me', workspace.slug],
    queryFn: async ({ signal }) => unwrap(await workspace.api.GET('/api/v1/me', { signal })),
    staleTime: 5 * 60_000,
  });
}

/** Every business the signed-in person belongs to, for switching; each keeps its own session. */
export function workspacesQuery(workspace: Workspace) {
  return queryOptions({
    queryKey: ['workspaces', workspace.slug],
    queryFn: async ({ signal }) => unwrap(await workspace.api.GET('/api/v1/me/workspaces', { signal })),
    staleTime: 5 * 60_000,
  });
}

/** The business of the current page, with its session and authenticated API client. */
export function useWorkspace(): Workspace {
  return workspaceRoute.useRouteContext({ select: (context) => context.workspace });
}

export function useCurrentUser() {
  return useSuspenseQuery(meQuery(useWorkspace())).data;
}

/** Owners and managers edit the menu; staff only mark dishes as sold out (the API enforces the same rule). */
export function useCanEditMenu(): boolean {
  const { role } = useCurrentUser();
  return role === 'Owner' || role === 'Manager';
}
