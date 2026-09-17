import type { PendingInvitationResponse, TeamMemberResponse } from '@armenu/api-client';
import { useQueryClient, useSuspenseQuery } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { useState } from 'react';
import { Menu, MenuItem, MenuTrigger, Popover } from 'react-aria-components';

import { formErrorsFrom } from '../../api/form-errors.ts';
import { meQuery, useCurrentUser, useWorkspace } from '../../app/workspace.ts';
import { formatDate } from '../../i18n/format.ts';
import { useI18n } from '../../i18n/i18n-context.ts';
import { Button } from '../../ui/Button.tsx';
import { MailIcon, MoreIcon, PlusIcon } from '../../ui/icons.tsx';
import { Card, PageHeader } from '../../ui/layout.tsx';
import { ConfirmDialog } from '../../ui/Modal.tsx';
import { PasswordConfirmDialog } from '../../ui/PasswordConfirmDialog.tsx';
import { useNotify } from '../../ui/toaster-context.ts';
import { InviteDialog } from './InviteDialog.tsx';
import { teamQuery, useTeamMutations } from './team-api.ts';

const menuItem = 'cursor-default rounded-md px-3 py-2 text-sm outline-none data-[focused]:bg-sunken';

type Confirmation =
  | { readonly kind: 'remove'; readonly member: TeamMemberResponse }
  | { readonly kind: 'revoke'; readonly invitation: PendingInvitationResponse };

export function TeamPage() {
  const { messages, language, describeError } = useI18n();
  const workspace = useWorkspace();
  const me = useCurrentUser();
  const { members, invitations } = useSuspenseQuery(teamQuery(workspace)).data;
  const { resend, revoke, changeRole, remove, transferOwnership } = useTeamMutations();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [successor, setSuccessor] = useState<TeamMemberResponse | undefined>();
  const notify = useNotify();
  const [isInviting, setIsInviting] = useState(false);
  const [confirmation, setConfirmation] = useState<Confirmation | undefined>();

  const roleName = (role: string) => messages.roles[role] ?? role;
  const failed = (error: unknown) => {
    notify(
      formErrorsFrom(error, { describe: describeError, networkError: messages.networkError }).form ??
        messages.unexpectedError,
      'error',
    );
  };

  const confirmed = confirmation?.kind === 'remove' ? remove : revoke;
  const confirm = () => {
    if (confirmation?.kind === 'remove') {
      remove.mutate(confirmation.member.id, {
        onSuccess: () => {
          notify(messages.memberRemoved(confirmation.member.fullName));
          setConfirmation(undefined);
        },
      });
    } else if (confirmation?.kind === 'revoke') {
      revoke.mutate(confirmation.invitation.id, {
        onSuccess: () => {
          notify(messages.invitationRevoked);
          setConfirmation(undefined);
        },
      });
    }
  };

  return (
    <>
      <PageHeader
        title={messages.navTeam}
        description={messages.teamIntro}
        actions={
          <Button
            variant="primary"
            onPress={() => {
              setIsInviting(true);
            }}
          >
            <PlusIcon />
            {messages.inviteMember}
          </Button>
        }
      />

      <div className="grid max-w-3xl grid-cols-1 gap-6">
        <Card>
          <h2 className="border-b border-line px-5 py-3 text-sm font-semibold">{messages.members}</h2>
          <ul className="divide-y divide-line">
            {members.map((member) => (
              <li key={member.id} className="flex items-center gap-3 px-5 py-3">
                <span
                  aria-hidden="true"
                  className="flex size-9 shrink-0 items-center justify-center rounded-full bg-accent-soft text-sm font-semibold text-accent"
                >
                  {initialsOf(member.fullName)}
                </span>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium">
                    {member.fullName}
                    {member.userId === me.id && (
                      <span className="font-normal text-ink-muted"> ({messages.you})</span>
                    )}
                  </p>
                  <p className="truncate text-xs text-ink-muted">
                    {member.email} · {messages.joinedOn(formatDate(member.joinedAt, language))}
                  </p>
                </div>
                <span className="rounded bg-sunken px-2 py-0.5 text-xs font-medium">
                  {roleName(member.role)}
                </span>
                {member.role === 'Owner' ? (
                  <span className="size-9" />
                ) : (
                  <MenuTrigger>
                    <Button
                      variant="ghost"
                      size="icon"
                      aria-label={`${messages.actions}: ${member.fullName}`}
                    >
                      <MoreIcon />
                    </Button>
                    <Popover
                      placement="bottom end"
                      className="min-w-44 rounded-lg border border-line bg-surface p-1 shadow-lg"
                    >
                      <Menu
                        className="outline-none"
                        onAction={(action) => {
                          if (action === 'remove') {
                            setConfirmation({ kind: 'remove', member });
                            return;
                          }

                          if (action === 'ownership') {
                            setSuccessor(member);
                            return;
                          }

                          const role = member.role === 'Manager' ? 'Staff' : 'Manager';
                          changeRole.mutate(
                            { membershipId: member.id, role },
                            {
                              onSuccess: () => {
                                notify(messages.roleChanged(member.fullName, roleName(role)));
                              },
                              onError: failed,
                            },
                          );
                        }}
                      >
                        <MenuItem id="role" className={menuItem}>
                          {messages.makeRole(member.role === 'Manager' ? 'Staff' : 'Manager')}
                        </MenuItem>
                        <MenuItem id="ownership" className={menuItem}>
                          {messages.transferOwnership}
                        </MenuItem>
                        <MenuItem id="remove" className={`${menuItem} text-danger`}>
                          {messages.removeMember}
                        </MenuItem>
                      </Menu>
                    </Popover>
                  </MenuTrigger>
                )}
              </li>
            ))}
          </ul>
        </Card>

        <Card>
          <h2 className="border-b border-line px-5 py-3 text-sm font-semibold">
            {messages.pendingInvitations}
          </h2>
          {invitations.length === 0 ? (
            <p className="px-5 py-6 text-sm text-ink-muted">{messages.noPendingInvitations}</p>
          ) : (
            <ul className="divide-y divide-line">
              {invitations.map((invitation) => (
                <li key={invitation.id} className="flex flex-wrap items-center gap-3 px-5 py-3">
                  <span
                    aria-hidden="true"
                    className="flex size-9 shrink-0 items-center justify-center rounded-full bg-sunken text-ink-muted"
                  >
                    <MailIcon />
                  </span>
                  <div className="min-w-48 flex-1">
                    <p className="truncate text-sm font-medium">
                      {invitation.email}{' '}
                      <span className="font-normal text-ink-muted">· {roleName(invitation.role)}</span>
                    </p>
                    <p className="truncate text-xs text-ink-muted">
                      {messages.invitedBy(invitation.invitedBy, formatDate(invitation.sentAt, language))} ·{' '}
                      {invitation.isExpired ? (
                        <span className="font-medium text-danger">{messages.invitationExpired}</span>
                      ) : (
                        messages.expiresOn(formatDate(invitation.expiresAt, language))
                      )}
                    </p>
                  </div>
                  <div className="ms-auto flex gap-1">
                    <Button
                      size="sm"
                      isPending={resend.isPending && resend.variables === invitation.id}
                      aria-label={`${messages.resendInvitation}: ${invitation.email}`}
                      onPress={() => {
                        resend.mutate(invitation.id, {
                          onSuccess: (sent) => {
                            if (sent.emailSent) {
                              notify(messages.invitationSent(invitation.email));
                            } else {
                              notify(messages.invitationNotEmailed, 'error');
                            }
                          },
                          onError: failed,
                        });
                      }}
                    >
                      {messages.resendInvitation}
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      aria-label={`${messages.revokeInvitation}: ${invitation.email}`}
                      onPress={() => {
                        setConfirmation({ kind: 'revoke', invitation });
                      }}
                    >
                      {messages.revokeInvitation}
                    </Button>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>

      {isInviting && (
        <InviteDialog
          onClose={() => {
            setIsInviting(false);
          }}
        />
      )}

      {successor !== undefined && (
        <PasswordConfirmDialog
          title={messages.transferOwnershipTitle(successor.fullName)}
          body={<p>{messages.transferOwnershipBody(successor.fullName)}</p>}
          confirmLabel={messages.transferOwnership}
          onClose={() => {
            setSuccessor(undefined);
          }}
          onConfirm={async (password) => {
            await transferOwnership.mutateAsync({ membershipId: successor.id, password });
            // A token issued now says "manager": the team page and owner actions go away.
            await workspace.session.renew();
            notify(messages.ownershipTransferred(successor.fullName));
            await queryClient.invalidateQueries({ queryKey: meQuery(workspace).queryKey });
            await navigate({ to: '/$workspace/menu', params: { workspace: workspace.slug } });
            queryClient.removeQueries({ queryKey: teamQuery(workspace).queryKey });
          }}
        />
      )}

      <ConfirmDialog
        isOpen={confirmation !== undefined}
        title={
          confirmation?.kind === 'remove'
            ? messages.removeMemberConfirm(confirmation.member.fullName)
            : confirmation?.kind === 'revoke'
              ? messages.revokeInvitationConfirm(confirmation.invitation.email)
              : ''
        }
        body={confirmation?.kind === 'remove' ? messages.removeMemberBody : messages.revokeInvitationBody}
        confirmLabel={confirmation?.kind === 'remove' ? messages.removeMember : messages.revokeInvitation}
        isPending={confirmed.isPending}
        error={
          confirmed.isError
            ? formErrorsFrom(confirmed.error, {
                describe: describeError,
                networkError: messages.networkError,
              }).form
            : undefined
        }
        onConfirm={confirm}
        onOpenChange={(isOpen) => {
          if (!isOpen) {
            remove.reset();
            revoke.reset();
            setConfirmation(undefined);
          }
        }}
      />
    </>
  );
}

function initialsOf(name: string): string {
  const words = name.trim().split(/\s+/);
  return ((words[0]?.[0] ?? '') + (words.length > 1 ? (words.at(-1)?.[0] ?? '') : '')).toLocaleUpperCase();
}
