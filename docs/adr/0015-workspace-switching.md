# ADR-0015: A person lists their own businesses through one narrow row-level security policy

- **Status:** Accepted
- **Date:** 2026-09-15

## Context

One account can belong to several businesses (an owner who also works a shift elsewhere). ADR-0006 left switching
between them for later because listing a person's memberships reads `tenant_memberships` across tenants, which
row-level security forbids for good reason.

## Decision

- A second, **`SELECT`-only** policy on `tenant_memberships`, `own_memberships`: rows whose `user_id` equals the
  transaction-local setting `app.current_user` are visible. Policies are permissive, so it adds exactly those rows; it
  grants no write and touches no other table.
- Only `GetMyWorkspacesQueryHandler` sets `app.current_user`, to the user id of the verified access token, with
  `set_config(…, true)` inside a transaction, so it ends with the transaction. It is the single query that turns off the
  tenant filter, and the comment on `QueryFilters.Tenant` says so.
- `GET /api/v1/me/workspaces` returns the active businesses with the role in each. The dashboard lists them in the
  account menu; opening one goes to that business, which asks for its own sign-in when there is no session.
- A guardrail test pins every policy other than `tenant_isolation` (today: exactly this one), and a row-level security
  test proves that naming a user shows only their memberships, allows no update, and reveals no other table.

## Consequences

- Sessions stay scoped to one business; switching does not mint tokens for another tenant, so a stolen access token
  still reaches one business.
- Rejected: a `SECURITY DEFINER` function (with `FORCE ROW LEVEL SECURITY` it is either useless or needs a role that
  bypasses isolation); a token exchange that signs the person into the other business automatically (widens what one
  stolen token can reach); storing memberships on the global user row (duplicated state).
