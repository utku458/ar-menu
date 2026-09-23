import { type ApiClient, createApiClient } from '@armenu/api-client';

import { unwrap } from '../api/result.ts';
import { env } from '../env.ts';
import { Session, type SessionTransport, type TokenGrant } from './session.ts';

/** A business the staff member works in: its session and an API client that authenticates with it. */
export interface Workspace {
  readonly slug: string;
  readonly session: Session;
  readonly api: ApiClient;
  /** True while the platform administrator is working here rather than a member of the business. */
  readonly isAdministered: boolean;
}

/** The workspace the platform administrator signs in to; it is not a business (see TenantSlug.Platform). */
export const platformSlug = 'system';

const workspaces = new Map<string, Workspace>();

export function workspaceFor(slug: string): Workspace {
  const key = slug.toLowerCase();
  let workspace = workspaces.get(key);
  if (workspace === undefined) {
    const session = new Session(key, httpTransport(key));
    workspace = { slug: key, session, api: authenticatedClient(session), isAdministered: false };
    workspaces.set(key, workspace);
  }

  return workspace;
}

/** Where the administrator's own session lives, and from which businesses are opened and entered. */
export function platformWorkspace(): Workspace {
  return workspaceFor(platformSlug);
}

/**
 * Starts working inside a business as the platform administrator, on the token the platform just issued.
 *
 * The session it gets back cannot be refreshed the usual way: there is no refresh cookie for a business one is not a
 * member of. Instead it enters again, which works for as long as the administrator's own session does — so the
 * access to a business really does end when the administrator signs out, rather than lasting a fortnight.
 */
export function administerBusiness(businessId: string, slug: string, grant: TokenGrant): Workspace {
  const key = slug.toLowerCase();
  const session = new Session(key, enterTransport(businessId));
  const workspace: Workspace = {
    slug: key,
    session,
    api: authenticatedClient(session),
    isAdministered: true,
  };

  workspaces.set(key, workspace);
  session.accept(grant);

  return workspace;
}

function enterTransport(businessId: string): SessionTransport {
  return {
    signIn() {
      throw new Error('An administered business is entered from the platform, not signed in to.');
    },
    async refresh() {
      const platform = platformWorkspace();
      if ((await platform.session.accessToken()) === undefined) {
        return undefined;
      }

      const result = await platform.api.POST('/api/v1/platform/businesses/{businessId}/enter', {
        params: { path: { businessId } },
      });

      return result.response.ok ? tokenGrant(unwrap(result)) : undefined;
    },
    async signOut() {
      // Nothing to end in the business: no session of ours lives there. Leaving is forgetting the token.
    },
  };
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

const tokenGrant = (response: { accessToken: string; expiresIn: number }): TokenGrant => ({
  accessToken: response.accessToken,
  expiresIn: response.expiresIn,
});

/**
 * Signs in with a user name, without naming a business: the account decides which one, and the answer says where the
 * session landed — a business, or the platform when this is the administrator.
 */
export async function signInWithUserName(
  userName: string,
  password: string,
): Promise<{ workspace: Workspace; isPlatformAdmin: boolean }> {
  const client = createApiClient(env.apiBaseUrl);
  const response = unwrap(
    await client.POST('/api/v1/auth/sign-in', {
      body: { userName, password },
      credentials: 'include',
    }),
  );

  const workspace = workspaceFor(response.workspace);
  workspace.session.accept(tokenGrant(response));

  return { workspace, isPlatformAdmin: response.isPlatformAdmin };
}

// Authentication calls carry the refresh cookie, which is scoped to this business's auth endpoints.
function httpTransport(slug: string): SessionTransport {
  const client = createApiClient(env.apiBaseUrl);
  const params = { path: { tenant: slug } };
  const grant = tokenGrant;

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
