import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, Outlet, useNavigate } from '@tanstack/react-router';
import { useEffect, useState, useSyncExternalStore } from 'react';
import { Header, Menu, MenuItem, MenuSection, MenuTrigger, Popover, Separator } from 'react-aria-components';

import { useI18n } from '../i18n/i18n-context.ts';
import { interfaceLanguages } from '../i18n/messages.ts';
import { env } from '../env.ts';
import { Button, LinkButton } from '../ui/Button.tsx';
import {
  ChevronDownIcon,
  ExternalLinkIcon,
  GlobeIcon,
  HistoryIcon,
  MenuListIcon,
  QrIcon,
  SettingsIcon,
  SignOutIcon,
  StatsIcon,
  UserIcon,
  UsersIcon,
} from '../ui/icons.tsx';
import { useNotify } from '../ui/toaster-context.ts';
import { meQuery, useCanEditMenu, useCurrentUser, useWorkspace, workspacesQuery } from './workspace.ts';

const navLink =
  'flex items-center gap-2 whitespace-nowrap rounded-lg px-3 py-2 text-sm font-medium text-ink-muted hover:bg-sunken hover:text-ink ' +
  'aria-[current=page]:bg-sunken aria-[current=page]:text-ink';

export function AppShell() {
  const { messages, language, setLanguage } = useI18n();
  const workspace = useWorkspace();
  const user = useCurrentUser();
  const canEdit = useCanEditMenu();
  const navigate = useNavigate();
  const [isSigningOut, setIsSigningOut] = useState(false);
  const queryClient = useQueryClient();
  const notify = useNotify();
  const otherWorkspaces = (useQuery(workspacesQuery(workspace)).data ?? []).filter(
    (candidate) => candidate.slug !== workspace.slug,
  );
  const resendVerification = useMutation({
    mutationFn: async () => {
      const { response } = await workspace.api.POST('/api/v1/me/email-verification', { body: { language } });
      if (!response.ok) {
        throw new Error(String(response.status));
      }
    },
    onSuccess: () => {
      notify(messages.verificationSent);
    },
    onError: () => {
      notify(messages.unexpectedError, 'error');
    },
  });

  // The address may have been confirmed in another tab: a fresh token says so when this one is looked at again.
  useEffect(() => {
    if (user.emailVerified) {
      return;
    }

    const recheck = () => {
      if (document.visibilityState === 'visible') {
        void workspace.session
          .renew()
          .then(() => queryClient.invalidateQueries({ queryKey: meQuery(workspace).queryKey }));
      }
    };
    document.addEventListener('visibilitychange', recheck);
    return () => {
      document.removeEventListener('visibilitychange', recheck);
    };
  }, [user.emailVerified, workspace, queryClient]);
  const isSignedIn = useSyncExternalStore(
    (listener) => workspace.session.subscribe(listener),
    () => workspace.session.isSignedIn,
  );

  // The API ended the session (revoked, expired, membership removed): back to signing in, with an explanation.
  useEffect(() => {
    if (!isSignedIn && !isSigningOut) {
      void navigate({
        to: '/$workspace/sign-in',
        params: { workspace: workspace.slug },
        search: { ended: true },
      });
    }
  }, [isSignedIn, isSigningOut, navigate, workspace.slug]);

  const signOut = async () => {
    setIsSigningOut(true);
    await workspace.session.signOut().catch(() => undefined);
    await navigate({ to: '/$workspace/sign-in', params: { workspace: workspace.slug } });
  };

  const params = { workspace: workspace.slug };

  return (
    <div className="min-h-dvh">
      <header data-print-hidden className="border-b border-line bg-surface">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-x-6 gap-y-2 px-4 py-3">
          <p className="flex items-center gap-2 font-semibold">
            <img src="/favicon.svg" alt="" className="size-6" />
            {user.tenant.name}
          </p>

          <nav
            aria-label={messages.mainNavigation}
            className="order-last -mx-1 flex w-full gap-1 overflow-x-auto sm:order-none sm:w-auto"
          >
            <Link to="/$workspace/menu" params={params} className={navLink}>
              <MenuListIcon />
              {messages.navMenu}
            </Link>
            <Link to="/$workspace/qr" params={params} className={navLink}>
              <QrIcon />
              {messages.navQrCodes}
            </Link>
            {canEdit && (
              <Link to="/$workspace/statistics" params={params} search={{ days: 7 }} className={navLink}>
                <StatsIcon />
                {messages.navStatistics}
              </Link>
            )}
            {canEdit && (
              <Link to="/$workspace/history" params={params} className={navLink}>
                <HistoryIcon />
                {messages.navHistory}
              </Link>
            )}
            {user.role === 'Owner' && (
              <Link to="/$workspace/team" params={params} className={navLink}>
                <UsersIcon />
                {messages.navTeam}
              </Link>
            )}
            {canEdit && (
              <Link to="/$workspace/settings" params={params} className={navLink}>
                <SettingsIcon />
                {messages.navSettings}
              </Link>
            )}
          </nav>

          <div className="ms-auto flex items-center gap-2">
            <LinkButton
              href={`${env.guestMenuBaseUrl}${workspace.slug}`}
              target="_blank"
              size="sm"
              variant="ghost"
            >
              <ExternalLinkIcon />
              <span className="hidden md:inline">{messages.viewGuestMenu}</span>
            </LinkButton>

            <MenuTrigger>
              <Button
                size="sm"
                variant="ghost"
                aria-label={`${user.fullName}, ${messages.roles[user.role] ?? user.role}`}
              >
                <span className="max-w-40 truncate">{user.fullName}</span>
                <ChevronDownIcon />
              </Button>
              <Popover className="min-w-56 rounded-lg border border-line bg-surface p-1 shadow-lg">
                <Menu className="outline-none">
                  <MenuItem isDisabled className="px-3 py-2 text-xs text-ink-muted">
                    {user.email} · {messages.roles[user.role] ?? user.role}
                  </MenuItem>
                  <Separator className="my-1 border-t border-line" />
                  <MenuItem
                    href={`/${workspace.slug}/account`}
                    className="flex cursor-default items-center gap-2 rounded-md px-3 py-2 text-sm outline-none data-[focused]:bg-sunken"
                  >
                    <UserIcon />
                    {messages.myAccount}
                  </MenuItem>
                  <Separator className="my-1 border-t border-line" />
                  {otherWorkspaces.length > 0 && (
                    <>
                      <MenuSection>
                        <Header className="px-3 pt-1 pb-1 text-xs font-medium text-ink-muted">
                          {messages.otherWorkspaces}
                        </Header>
                        {otherWorkspaces.map((other) => (
                          <MenuItem
                            key={other.slug}
                            href={`/${other.slug}`}
                            textValue={other.name}
                            className="flex cursor-default items-center gap-2 rounded-md px-3 py-2 text-sm outline-none data-[focused]:bg-sunken"
                          >
                            <span className="truncate">{other.name}</span>
                            <span className="ms-auto text-xs text-ink-muted">
                              {messages.roles[other.role] ?? other.role}
                            </span>
                          </MenuItem>
                        ))}
                      </MenuSection>
                      <Separator className="my-1 border-t border-line" />
                    </>
                  )}
                  {interfaceLanguages.map((code) => (
                    <MenuItem
                      key={code}
                      onAction={() => {
                        setLanguage(code);
                      }}
                      className="flex cursor-default items-center gap-2 rounded-md px-3 py-2 text-sm outline-none data-[focused]:bg-sunken"
                    >
                      <GlobeIcon />
                      <span lang={code}>{code === 'tr' ? 'Türkçe' : 'English'}</span>
                      {code === language && <span className="ms-auto text-xs text-ink-muted">✓</span>}
                    </MenuItem>
                  ))}
                  <Separator className="my-1 border-t border-line" />
                  <MenuItem
                    onAction={() => void signOut()}
                    className="flex cursor-default items-center gap-2 rounded-md px-3 py-2 text-sm outline-none data-[focused]:bg-sunken"
                  >
                    <SignOutIcon />
                    {messages.signOut}
                  </MenuItem>
                </Menu>
              </Popover>
            </MenuTrigger>
          </div>
        </div>
      </header>

      {/*
        The administrator is not a member here and this session cannot be refreshed the ordinary way, so the way back
        to the platform has to be on screen rather than in the browser's history.
      */}
      {workspace.isAdministered && (
        <div data-print-hidden role="status" className="border-b border-line bg-accent-soft">
          <p className="mx-auto flex max-w-6xl flex-wrap items-center gap-x-4 gap-y-2 px-4 py-2 text-sm">
            <span>{messages.administeringBusiness}</span>
            <Link to="/platform" className="font-medium text-accent underline-offset-4 hover:underline">
              {messages.backToPlatform}
            </Link>
          </p>
        </div>
      )}

      {/* A user-name account has a placeholder address no mail reaches: there is nothing for it to verify. */}
      {!user.emailVerified && user.userName === null && (
        <div data-print-hidden role="status" className="border-b border-line bg-accent-soft">
          <p className="mx-auto flex max-w-6xl flex-wrap items-center gap-x-4 gap-y-2 px-4 py-2 text-sm">
            <span>{messages.verifyEmailBanner(user.email)}</span>
            <Button
              size="sm"
              variant="secondary"
              isPending={resendVerification.isPending}
              onPress={() => {
                resendVerification.mutate();
              }}
            >
              {messages.resendVerification}
            </Button>
          </p>
        </div>
      )}

      <main className="mx-auto max-w-6xl px-4 py-8">
        <Outlet />
      </main>
    </div>
  );
}
