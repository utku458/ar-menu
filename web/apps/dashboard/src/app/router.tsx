import type { QueryClient } from '@tanstack/react-query';
import {
  createRootRouteWithContext,
  createRoute,
  createRouter,
  Outlet,
  redirect,
} from '@tanstack/react-router';

import { readLastWorkspace, saveLastWorkspace } from '../auth/last-workspace.ts';
import { workspaceFor } from '../auth/workspaces.ts';
import { managedMenuQuery } from '../features/menu/menu-api.ts';
import { MenuEditorPage } from '../features/menu/MenuEditorPage.tsx';
import { QrCodesPage } from '../features/qr/QrCodesPage.tsx';
import { AccountPage } from '../features/account/AccountPage.tsx';
import { HistoryPage } from '../features/history/HistoryPage.tsx';
import { SettingsPage } from '../features/settings/SettingsPage.tsx';
import { type StatisticsPeriod, statisticsPeriods } from '../features/statistics/statistics-api.ts';
import { StatisticsPage } from '../features/statistics/StatisticsPage.tsx';
import { teamQuery } from '../features/team/team-api.ts';
import { TeamPage } from '../features/team/TeamPage.tsx';
import { ForgotPasswordPage } from '../pages/ForgotPasswordPage.tsx';
import { JoinPage } from '../pages/JoinPage.tsx';
import { ResetPasswordPage } from '../pages/ResetPasswordPage.tsx';
import { VerifyEmailPage } from '../pages/VerifyEmailPage.tsx';
import { NotFoundPage, RouteError } from '../pages/NotFoundPage.tsx';
import { SignInPage } from '../pages/SignInPage.tsx';
import { SignUpPage } from '../pages/SignUpPage.tsx';
import { WorkspacePage } from '../pages/WorkspacePage.tsx';
import { AppShell } from './AppShell.tsx';
import { meQuery } from './workspace.ts';

export interface RouterContext {
  readonly queryClient: QueryClient;
}

const rootRoute = createRootRouteWithContext<RouterContext>()({
  component: Outlet,
  notFoundComponent: NotFoundPage,
});

const startRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  validateSearch: (search: Record<string, unknown>): { choose?: boolean | undefined } => ({
    choose: search.choose === true || search.choose === 'true' ? true : undefined,
  }),
  beforeLoad: ({ search }) => {
    const last = readLastWorkspace();
    if (last !== undefined && search.choose !== true) {
      // eslint-disable-next-line @typescript-eslint/only-throw-error -- redirects are thrown by design in TanStack Router.
      throw redirect({ to: '/$workspace', params: { workspace: last } });
    }
  },
  component: WorkspacePage,
});

const signUpRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: 'signup',
  component: SignUpPage,
});

const signInRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '$workspace/sign-in',
  validateSearch: (
    search: Record<string, unknown>,
  ): { redirect?: string | undefined; ended?: boolean | undefined } => ({
    redirect: typeof search.redirect === 'string' ? search.redirect : undefined,
    ended: search.ended === true || search.ended === 'true' ? true : undefined,
  }),
  beforeLoad: ({ params }) => {
    if (workspaceFor(params.workspace).session.isSignedIn) {
      // eslint-disable-next-line @typescript-eslint/only-throw-error -- redirects are thrown by design in TanStack Router.
      throw redirect({ to: '/$workspace/menu', params });
    }
  },
  component: SignInPage,
});

const forgotPasswordRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: 'forgot-password',
  validateSearch: (search: Record<string, unknown>): { workspace?: string | undefined } => ({
    workspace: typeof search.workspace === 'string' ? search.workspace : undefined,
  }),
  component: ForgotPasswordPage,
});

/** Where the links of account e-mails land: public, the secret is in the fragment. */
const resetPasswordRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: 'reset-password',
  component: ResetPasswordPage,
});

const verifyEmailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: 'verify-email',
  component: VerifyEmailPage,
});

/** Where an invitation link lands: public, since the person joining has no session in this business yet. */
const joinRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '$workspace/join',
  component: JoinPage,
});

/** Everything under a business requires its session; a reload restores it from the refresh cookie. */
const workspaceRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '$workspace',
  beforeLoad: async ({ params, location }) => {
    const workspace = workspaceFor(params.workspace);
    if ((await workspace.session.accessToken()) === undefined) {
      // eslint-disable-next-line @typescript-eslint/only-throw-error -- redirects are thrown by design in TanStack Router.
      throw redirect({ to: '/$workspace/sign-in', params, search: { redirect: location.href } });
    }

    saveLastWorkspace(workspace.slug);
    return { workspace };
  },
  loader: ({ context }) => context.queryClient.query({ ...meQuery(context.workspace), staleTime: 'static' }),
  component: AppShell,
  errorComponent: RouteError,
});

const workspaceIndexRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: '/',
  beforeLoad: ({ params }) => {
    // eslint-disable-next-line @typescript-eslint/only-throw-error -- redirects are thrown by design in TanStack Router.
    throw redirect({ to: '/$workspace/menu', params });
  },
});

const menuRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: 'menu',
  validateSearch: (search: Record<string, unknown>): { category?: string | undefined } => ({
    category: typeof search.category === 'string' ? search.category : undefined,
  }),
  loader: ({ context }) =>
    context.queryClient.query({ ...managedMenuQuery(context.workspace), staleTime: 'static' }),
  component: MenuEditorPage,
});

const qrRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: 'qr',
  component: QrCodesPage,
});

const settingsRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: 'settings',
  component: SettingsPage,
});

/** Where languages lived before settings grew; kept so bookmarks still work. */
const languagesRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: 'languages',
  beforeLoad: ({ params }) => {
    // eslint-disable-next-line @typescript-eslint/only-throw-error -- redirects are thrown by design in TanStack Router.
    throw redirect({ to: '/$workspace/settings', params });
  },
});

/** Every member manages their own account, whatever their role. */
const accountRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: 'account',
  component: AccountPage,
});

/** For owners and managers, like statistics; the API refuses staff too. */
const historyRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: 'history',
  beforeLoad: async ({ context, params }) => {
    const me = await context.queryClient.query({ ...meQuery(context.workspace), staleTime: 'static' });
    if (me.role === 'Staff') {
      // eslint-disable-next-line @typescript-eslint/only-throw-error -- redirects are thrown by design in TanStack Router.
      throw redirect({ to: '/$workspace/menu', params });
    }
  },
  component: HistoryPage,
});

/** For owners and managers, like menu editing; the API refuses staff too. */
const statisticsRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: 'statistics',
  validateSearch: (search: Record<string, unknown>): { days: StatisticsPeriod } => ({
    days: statisticsPeriods.find((period) => String(period) === String(search.days)) ?? 7,
  }),
  beforeLoad: async ({ context, params }) => {
    const me = await context.queryClient.query({ ...meQuery(context.workspace), staleTime: 'static' });
    if (me.role === 'Staff') {
      // eslint-disable-next-line @typescript-eslint/only-throw-error -- redirects are thrown by design in TanStack Router.
      throw redirect({ to: '/$workspace/menu', params });
    }
  },
  component: StatisticsPage,
});

/** The owner's page; the API refuses everyone else too. */
const teamRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: 'team',
  beforeLoad: async ({ context, params }) => {
    const me = await context.queryClient.query({ ...meQuery(context.workspace), staleTime: 'static' });
    if (me.role !== 'Owner') {
      // eslint-disable-next-line @typescript-eslint/only-throw-error -- redirects are thrown by design in TanStack Router.
      throw redirect({ to: '/$workspace/menu', params });
    }
  },
  loader: ({ context }) =>
    context.queryClient.query({ ...teamQuery(context.workspace), staleTime: 'static' }),
  component: TeamPage,
});

export function createAppRouter(queryClient: QueryClient) {
  return createRouter({
    routeTree: rootRoute.addChildren([
      startRoute,
      signUpRoute,
      signInRoute,
      joinRoute,
      forgotPasswordRoute,
      resetPasswordRoute,
      verifyEmailRoute,
      workspaceRoute.addChildren([
        workspaceIndexRoute,
        menuRoute,
        qrRoute,
        settingsRoute,
        languagesRoute,
        statisticsRoute,
        historyRoute,
        teamRoute,
        accountRoute,
      ]),
    ]),
    context: { queryClient },
    defaultPreload: 'intent',
  });
}

declare module '@tanstack/react-router' {
  interface Register {
    router: ReturnType<typeof createAppRouter>;
  }
}
