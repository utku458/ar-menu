import type { AffectedWorkspace } from '@armenu/api-client';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { type ReactNode, useState } from 'react';

import { ensureOk } from '../../api/result.ts';
import { useCurrentUser, useWorkspace } from '../../app/workspace.ts';
import { forgetLastWorkspace } from '../../auth/last-workspace.ts';
import { useI18n } from '../../i18n/i18n-context.ts';
import { Button, LinkButton } from '../../ui/Button.tsx';
import { Card, PageHeader } from '../../ui/layout.tsx';
import { ChangePasswordCard } from './ChangePasswordCard.tsx';
import { PasswordConfirmDialog } from '../../ui/PasswordConfirmDialog.tsx';
import { useNotify } from '../../ui/toaster-context.ts';
import { accountDeletionQuery } from './account-api.ts';

export function AccountPage() {
  const { messages, language } = useI18n();
  const workspace = useWorkspace();
  const user = useCurrentUser();
  const navigate = useNavigate();
  const notify = useNotify();
  const deletion = useQuery(accountDeletionQuery(workspace));
  const [isConfirming, setIsConfirming] = useState(false);
  const blocked = (deletion.data?.workspacesToHandOver.length ?? 0) > 0;

  const deleteAccount = async (password: string) => {
    ensureOk(await workspace.api.POST('/api/v1/me/deletion', { body: { password, language } }));
    // Leave the business first: its shell would otherwise read the ended session as "signed out elsewhere".
    forgetLastWorkspace();
    await navigate({ to: '/', search: { choose: true } });
    workspace.session.end();
    notify(messages.accountDeleted);
  };

  return (
    <>
      <PageHeader title={messages.myAccount} description={messages.accountIntro} />

      <div className="grid max-w-2xl grid-cols-1 gap-6">
        <Card className="p-5">
          <dl className="grid grid-cols-1 gap-3 text-sm sm:grid-cols-[max-content_1fr] sm:gap-x-6">
            <dt className="text-ink-muted">{messages.fullName}</dt>
            <dd className="font-medium">{user.fullName}</dd>
            {/* A user-name account signs in with its name; its address is a placeholder and would only confuse. */}
            <dt className="text-ink-muted">{user.userName === null ? messages.email : messages.userName}</dt>
            <dd className="font-medium break-all">{user.userName ?? user.email}</dd>
          </dl>
        </Card>

        <ChangePasswordCard workspace={workspace} />

        <Card className="p-5">
          <h2 className="text-base font-semibold">{messages.deleteAccount}</h2>
          <p className="mt-1 text-sm text-ink-muted">{messages.deleteAccountIntro}</p>

          {deletion.data === undefined ? (
            <p role="status" className="mt-4 text-sm text-ink-muted">
              {deletion.isError ? messages.networkError : messages.loading}
            </p>
          ) : (
            <>
              <WorkspaceList
                title={messages.workspacesToHandOver}
                body={messages.workspacesToHandOverBody}
                workspaces={deletion.data.workspacesToHandOver}
                tone="danger"
                action={(affected) => (
                  <LinkButton size="sm" href={`/${affected.slug}/team`}>
                    {messages.handOverIn(affected.name)}
                  </LinkButton>
                )}
              />
              <WorkspaceList
                title={messages.closingWorkspaces}
                body={messages.closingWorkspacesBody}
                workspaces={deletion.data.closingWorkspaces}
                tone="danger"
              />
              <WorkspaceList
                title={messages.workspacesToLeave}
                workspaces={deletion.data.workspacesToLeave}
              />

              <div className="mt-5 flex justify-end border-t border-line pt-4">
                <Button
                  variant="danger"
                  isDisabled={blocked}
                  onPress={() => {
                    setIsConfirming(true);
                  }}
                >
                  {messages.deleteAccount}
                </Button>
              </div>
            </>
          )}
        </Card>
      </div>

      {isConfirming && (
        <PasswordConfirmDialog
          title={messages.deleteAccountConfirm}
          body={
            <>
              <p>{messages.deleteAccountIntro}</p>
              {(deletion.data?.closingWorkspaces.length ?? 0) > 0 && (
                <p className="font-medium text-danger">
                  {messages.closingWorkspaces}:{' '}
                  {deletion.data?.closingWorkspaces.map((affected) => affected.name).join(', ')}
                </p>
              )}
              <p>{messages.deleteAccountConfirmBody}</p>
            </>
          }
          confirmLabel={messages.deleteAccount}
          onConfirm={deleteAccount}
          onClose={() => {
            setIsConfirming(false);
          }}
        />
      )}
    </>
  );
}

function WorkspaceList({
  title,
  body,
  workspaces,
  tone,
  action,
}: {
  title: string;
  body?: string;
  workspaces: readonly AffectedWorkspace[];
  tone?: 'danger';
  action?: (workspace: AffectedWorkspace) => ReactNode;
}) {
  const { messages } = useI18n();
  if (workspaces.length === 0) {
    return null;
  }

  return (
    <section className="mt-5">
      <h3 className={`text-sm font-semibold ${tone === 'danger' ? 'text-danger' : ''}`}>{title}</h3>
      {body !== undefined && <p className="mt-1 text-sm text-ink-muted">{body}</p>}
      <ul className="mt-2 divide-y divide-line rounded-lg border border-line">
        {workspaces.map((affected) => (
          <li key={affected.slug} className="flex flex-wrap items-center gap-x-3 gap-y-2 px-3 py-2 text-sm">
            <span className="font-medium">{affected.name}</span>
            {affected.otherMembers > 0 && (
              <span className="text-xs text-ink-muted">{messages.teamSize(affected.otherMembers)}</span>
            )}
            {action !== undefined && <span className="ms-auto">{action(affected)}</span>}
          </li>
        ))}
      </ul>
    </section>
  );
}
