import { fileURLToPath } from 'node:url';

import type { PublicMenuResponse } from '@armenu/api-client';
import { test as base, expect, type Page } from '@playwright/test';

import { apiOrigin, assetOrigin, burgerLab, seasideGrill } from './menus.ts';

const assetRoot = fileURLToPath(new URL('../../../../../assets/', import.meta.url));
const cors = { 'Access-Control-Allow-Origin': '*' };

/** Stands in for the API: serves menu fixtures, records requests and can fail on demand. */
export class MenuApi {
  readonly requests: { readonly method: string; readonly url: URL }[] = [];
  /** Batches the app reported to the events endpoint, as sent. */
  readonly eventBatches: { readonly tenant: string; readonly events: unknown }[] = [];
  private readonly failures: number[] = [];
  private readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  /** The next menu requests answer with these statuses, in order. */
  failNextRequests(...statuses: number[]): void {
    this.failures.push(...statuses);
  }

  menuRequests(): URL[] {
    return this.requests.filter((request) => request.method === 'GET').map((request) => request.url);
  }

  async install(): Promise<void> {
    await this.page.route(`${apiOrigin}/api/v1/menus/*`, async (route) => {
      const request = route.request();
      const url = new URL(request.url());
      this.requests.push({ method: request.method(), url });

      const failure = this.failures.shift();
      if (failure !== undefined) {
        await route.fulfill({
          status: failure,
          headers: cors,
          json: { status: failure, code: 'test.failure' },
        });
        return;
      }

      const menu = findMenu(
        decodeURIComponent(url.pathname.split('/').at(-1) ?? ''),
        url.searchParams.get('lang'),
      );
      await (menu === undefined
        ? route.fulfill({ status: 404, headers: cors, json: { status: 404, code: 'tenant.not_found' } })
        : route.fulfill({ status: 200, headers: cors, json: menu }));
    });

    // Registered later, so it wins over the menu route for its own path.
    await this.page.route(`${apiOrigin}/api/v1/menus/*/events`, async (route) => {
      const request = route.request();
      const headers = {
        ...cors,
        'Access-Control-Allow-Headers': 'content-type',
        'Access-Control-Allow-Methods': 'POST',
      };
      if (request.method() === 'POST') {
        const tenant = new URL(request.url()).pathname.split('/').at(-2) ?? '';
        this.eventBatches.push({ tenant, events: (request.postDataJSON() as { events: unknown }).events });
      }
      await route.fulfill({ status: 204, headers });
    });

    // Real demo models and posters, so <model-viewer> genuinely downloads and renders them.
    await this.page.route(`${assetOrigin}/**`, (route) =>
      route.fulfill({
        path: `${assetRoot}${new URL(route.request().url()).pathname.slice(1)}`,
        headers: cors,
      }),
    );
  }
}

function findMenu(slug: string, lang: string | null): PublicMenuResponse | undefined {
  switch (slug) {
    case 'kadikoy-burger-lab':
      return burgerLab(lang === 'en' ? 'en' : 'tr');
    case 'seaside-grill':
      return seasideGrill();
    default:
      return undefined;
  }
}

export const test = base.extend<{
  api: MenuApi;
  thirdPartyRequests: string[];
  contentSecurityPolicy: string[];
}>({
  // Pages are served with the production security headers (vite preview); nothing the app does may break its policy.
  contentSecurityPolicy: [
    async ({ page }, use) => {
      const violations: string[] = [];
      const unprotectedPages: string[] = [];
      page.on('console', (message) => {
        if (message.text().includes('Content Security Policy')) {
          violations.push(message.text());
        }
      });
      page.on('response', (response) => {
        if (
          response.request().resourceType() === 'document' &&
          response.headers()['content-security-policy'] === undefined
        ) {
          unprotectedPages.push(response.url());
        }
      });

      await use(violations);

      expect(unprotectedPages, 'pages served without a content security policy').toEqual([]);
      expect(violations, 'content security policy violations').toEqual([]);
    },
    { auto: true },
  ],

  // Guests' phones talk to the app, the API and the asset CDN only: no fonts, trackers or decoder downloads.
  thirdPartyRequests: [
    async ({ page, baseURL }, use) => {
      const allowed = new Set([new URL(baseURL ?? '').origin, apiOrigin, assetOrigin]);
      const unexpected: string[] = [];
      page.on('request', (request) => {
        const url = new URL(request.url());
        if (url.protocol.startsWith('http') && !allowed.has(url.origin)) {
          unexpected.push(request.url());
        }
      });

      await use(unexpected);

      expect(unexpected, 'requests to unexpected origins').toEqual([]);
    },
    { auto: true },
  ],

  // Every test runs against the stand-in API, whether or not it inspects it.
  api: [
    async ({ page }, use) => {
      const api = new MenuApi(page);
      await api.install();
      await use(api);
    },
    { auto: true },
  ],
});

export { expect };

/** Polls a condition without failing the test, for use inside route handlers. */
export async function eventually(condition: () => boolean, timeoutMs = 3_000): Promise<boolean> {
  const deadline = Date.now() + timeoutMs;
  while (!condition()) {
    if (Date.now() > deadline) {
      return false;
    }
    await new Promise((resolve) => setTimeout(resolve, 20));
  }

  return true;
}
