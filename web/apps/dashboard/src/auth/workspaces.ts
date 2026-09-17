import { type ApiClient, createApiClient } from '@armenu/api-client';

import { unwrap } from '../api/result.ts';
import { env } from '../env.ts';
import { Session, type SessionTransport, type TokenGrant } from './session.ts';

/** A business the staff member works in: its session and an API client that authenticates with it. */
export interface Workspace {
  readonly slug: string;
  readonly session: Session;
  readonly api: ApiClient;
}

const workspaces = new Map<string, Workspace>();

export function workspaceFor(slug: string): Workspace {
  const key = slug.toLowerCase();
  let workspace = workspaces.get(key);
  if (workspace === undefined) {
    const session = new Session(key, httpTransport(key));
    workspace = { slug: key, session, api: authenticatedClient(session) };
    workspaces.set(key, workspace);
  }

  return workspace;
}

function authenticatedClient(session: Session): ApiClient {
  const api = createApiClient(env.apiBaseUrl);
  api.use({
    async onRequest({ request }) {
      const token = await session.accessToken();
      if (token !== undefined) {
        request.headers.set('Authorization', `Bearer ${token}`);
      }

      return request;
    },
    onResponse({ response }) {
      // A token the API no longer accepts means the session was revoked or the membership removed.
      if (response.status === 401) {
        session.end();
      }

      return response;
    },
  });

  return api;
}

// Authentication calls carry the refresh cookie, which is scoped to this business's auth endpoints.
function httpTransport(slug: string): SessionTransport {
  const client = createApiClient(env.apiBaseUrl);
  const params = { path: { tenant: slug } };
  const grant = (response: { accessToken: string; expiresIn: number }): TokenGrant => ({
    accessToken: response.accessToken,
    expiresIn: response.expiresIn,
  });

  return {
    async signIn(email, password) {
      return grant(
        unwrap(
          await client.POST('/api/v1/tenants/{tenant}/auth/sign-in', {
            params,
            body: { email, password },
            credentials: 'include',
          }),
        ),
      );
    },
    async refresh() {
      const result = await client.POST('/api/v1/tenants/{tenant}/auth/refresh', {
        params,
        credentials: 'include',
      });
      return result.response.status === 401 || result.response.status === 404
        ? undefined
        : grant(unwrap(result));
    },
    async signOut() {
      await client.POST('/api/v1/tenants/{tenant}/auth/sign-out', { params, credentials: 'include' });
    },
  };
}
