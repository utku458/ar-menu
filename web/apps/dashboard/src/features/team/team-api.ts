import type { InvitationRequest, TeamRole } from '@armenu/api-client';
import { queryOptions, useMutation, useQueryClient } from '@tanstack/react-query';

import { ensureOk, unwrap } from '../../api/result.ts';
import { useWorkspace } from '../../app/workspace.ts';
import type { Workspace } from '../../auth/workspaces.ts';
import { useI18n } from '../../i18n/i18n-context.ts';

export function teamQuery(workspace: Workspace) {
  return queryOptions({
    queryKey: ['team', workspace.slug],
    queryFn: async ({ signal }) => unwrap(await workspace.api.GET('/api/v1/manage/team', { signal })),
  });
}

/** Team changes; each refreshes the team afterwards. E-mails are written in the owner's interface language. */
export function useTeamMutations() {
  const workspace = useWorkspace();
  const { api } = workspace;
  const { language } = useI18n();
  const queryClient = useQueryClient();
  const refresh = () => queryClient.invalidateQueries({ queryKey: teamQuery(workspace).queryKey });

  return {
    invite: useMutation({
      mutationFn: async (body: Omit<InvitationRequest, 'language'>) =>
        unwrap(await api.POST('/api/v1/manage/team/invitations', { body: { ...body, language } })),
      onSuccess: refresh,
    }),
    resend: useMutation({
      mutationFn: async (invitationId: string) =>
        unwrap(
          await api.POST('/api/v1/manage/team/invitations/{invitationId}/resend', {
            params: { path: { invitationId } },
            body: { language },
          }),
        ),
      onSuccess: refresh,
    }),
    revoke: useMutation({
      mutationFn: async (invitationId: string) => {
        ensureOk(
          await api.DELETE('/api/v1/manage/team/invitations/{invitationId}', {
            params: { path: { invitationId } },
          }),
        );
      },
      onSuccess: refresh,
    }),
    changeRole: useMutation({
      mutationFn: async ({ membershipId, role }: { membershipId: string; role: TeamRole }) => {
        ensureOk(
          await api.PUT('/api/v1/manage/team/members/{membershipId}/role', {
            params: { path: { membershipId } },
            body: { role },
          }),
        );
      },
      onSuccess: refresh,
    }),
    transferOwnership: useMutation({
      mutationFn: async ({ membershipId, password }: { membershipId: string; password: string }) => {
        ensureOk(
          await api.POST('/api/v1/manage/team/members/{membershipId}/ownership', {
            params: { path: { membershipId } },
            body: { password },
          }),
        );
      },
    }),
    remove: useMutation({
      mutationFn: async (membershipId: string) => {
        ensureOk(
          await api.DELETE('/api/v1/manage/team/members/{membershipId}', {
            params: { path: { membershipId } },
          }),
        );
      },
      onSuccess: refresh,
    }),
  };
}
