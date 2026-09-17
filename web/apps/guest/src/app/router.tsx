import {
  createRootRoute,
  createRoute,
  createRouter,
  Outlet,
  parseSearchWith,
  stringifySearchWith,
} from '@tanstack/react-router';

import { MenuPage } from '../features/menu/MenuPage.tsx';
import { HomePage } from '../pages/HomePage.tsx';
import { NotFoundPage } from '../pages/NotFoundPage.tsx';

export interface MenuSearch {
  /** Language the guest picked; otherwise the API chooses from the browser language. */
  readonly lang?: string | undefined;
  /** The dish whose sheet is open. In the URL so the back button closes it and links can point at a dish. */
  readonly item?: string | undefined;
  /** Table label from a table's QR code; kept in the URL while the guest browses. */
  readonly table?: string | undefined;
}

const rootRoute = createRootRoute({
  component: Outlet,
  notFoundComponent: NotFoundPage,
});

const homeRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  component: HomePage,
});

const menuRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '$tenant',
  validateSearch: (search: Record<string, unknown>): MenuSearch => ({
    lang: nonEmptyString(search.lang),
    item: nonEmptyString(search.item),
    table: tableLabel(search.table),
  }),
  component: MenuPage,
});

export const router = createRouter({
  routeTree: rootRoute.addChildren([homeRoute, menuRoute]),
  basepath: import.meta.env.BASE_URL,
  scrollRestoration: true,
  // Plain query strings (?table=12), as printed in QR codes, instead of JSON-encoded values (?table=%2212%22).
  parseSearch: parseSearchWith((value) => value),
  stringifySearch: stringifySearchWith((value: unknown) => String(value)),
});

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }

  interface HistoryState {
    /** Set when a dish sheet was opened from the menu, so closing it can go back instead of adding history. */
    readonly openedFromMenu?: boolean;
  }
}

// Short labels only ("12", "A4", "Bahçe 3"): the value is shown on the page.
function tableLabel(value: unknown): string | undefined {
  const label = nonEmptyString(value)?.trim();
  return label !== undefined && /^[\p{L}\p{N} -]{1,12}$/u.test(label) ? label : undefined;
}

// The router decodes "12" in a query string as the number 12; every parameter here is text.
function nonEmptyString(value: unknown): string | undefined {
  const text = typeof value === 'string' || typeof value === 'number' ? String(value) : undefined;
  return text !== undefined && text.trim() !== '' ? text : undefined;
}
