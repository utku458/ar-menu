# ADR-0006: Workspace sign-in, tenant-scoped JWTs and rotating refresh tokens

- **Status:** Accepted
- **Date:** 2026-09-14

## Context

Restaurant staff sign in to a browser-based management panel. A person can belong to more than one business, and
permissions differ per business. Row-level security (ADR-0003) means identity data used during sign-in must not require
reading across tenants. We considered ASP.NET Core Identity, an external identity provider, and a lean in-house model.
Identity's global roles and table layout fit per-tenant roles poorly, and an external provider adds cost and a
dependency before the product has customers.

## Decision

### Model

| Entity | Scope | Purpose |
| --- | --- | --- |
| `User` | Global | One identity per person (e-mail, name, password hash, lockout state). No `tenant_id`, no row-level security; reached only by id or e-mail. |
| `TenantMembership` | Tenant, RLS | User plus role (`Owner`, `Manager`, `Staff`). Unique per user per tenant. |
| `UserSession` | Tenant, RLS | One row per sign-in session with the current and previous refresh token hash. A composite FK to the membership cascades: removing a member ends their sessions. |

### Workspace sign-in

Authentication endpoints live under the tenant slug (`/api/v1/tenants/{slug}/auth/…`), the way Slack and Atlassian
workspaces work. The tenant is resolved from the URL first, so the membership check and the session write happen inside
that tenant's scope, and sign-in needs no cross-tenant read. Being registered is not enough: the account must be a member
of that workspace.

### Tokens

- **Access token:** JWT signed with HS256; the algorithm is pinned during validation. Lifetime is 15 minutes, because
  access tokens cannot be revoked. Claims are `sub`, `email`, `name`, `tenant_id`, `role`, `sid` and `jti`. Every token is
  scoped to exactly one tenant, and management endpoints take the tenant from `tenant_id`, never from the URL or body.
- **Refresh token:** `{sessionId}.{256-bit secret}`, delivered only in an `HttpOnly; Secure; SameSite=Strict` cookie whose
  path is the workspace's auth endpoints. Scripts cannot read it, and cross-site requests do not send it. Sessions of
  different workspaces coexist in one browser. Only the SHA-256 hash of the secret is stored, and it is compared in
  constant time. Non-canonical base64url spellings of a secret are rejected.
- **Rotation and reuse detection:** every refresh replaces the secret. Presenting any replaced secret revokes the session
  and logs a warning, because two parties holding one token means it was stolen. The token replaced by the latest
  rotation is rejected without revocation for 30 seconds, since two tabs refreshing together is normal. Sessions expire
  after 14 idle days and 30 days at most.
- **Re-evaluation on refresh:** role changes take effect and removed members lose access at the next refresh.

### Credentials

- **Hashing:** PBKDF2-HMAC-SHA512 through the platform implementation, with 210,000 iterations (OWASP) and a
  self-describing format. Hashes with fewer iterations are upgraded at the next successful sign-in.
- **Password policy:** at least 12 characters (OWASP ASVS), at most 128, and no composition rules.
- **Lockout:** after 5 consecutive failures the account is locked for 15 minutes. A locked account is not even checked,
  so guessing cannot continue during the lockout.
- **No enumeration:** unknown account, wrong password, lockout and "not a member" return the same 401 after the same
  hashing work (decoy verification).

### Platform protections

- A fallback authorization policy requires an authenticated user on every endpoint not marked `AllowAnonymous()`.
- Rate limits per client: 10 authentication requests per minute, 5 sign-ups per hour, and a token bucket for public menus
  sized for a full restaurant behind one Wi-Fi address.
- Token responses carry `Cache-Control: no-store`. Commands, request DTOs and token types override `ToString()` so
  secrets never reach logs.
- Signing key, issuer, audience and lifetimes are validated at startup. A missing or weak key stops the application.

## Consequences

- Users must know their workspace slug (the sign-up response and future invitation e-mails provide it). A cross-workspace
  switcher needed a dedicated read path; ADR-0015 added it.
- An owner of two businesses cannot reuse the same e-mail for a second sign-up yet. An existing account joins another
  business through an invitation ([ADR-0013](0013-team-invitations.md)).
- Lockout can be abused to lock out a known account. Rate limiting mitigates this; progressive delays or CAPTCHA are
  future options.
- Production needs the panel and the API on the same site (e.g. `app.armenu.app` and `api.armenu.app`) for
  `SameSite=Strict` cookies, and a secret store for the signing key. Rotating the key currently invalidates outstanding
  access tokens (at most 15 minutes).
- Not in scope yet: multi-factor authentication. Staff invitations arrived with ADR-0013, password reset and e-mail
  verification with ADR-0014.
