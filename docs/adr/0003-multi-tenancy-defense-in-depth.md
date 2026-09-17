# ADR-0003: Shared-schema multi-tenancy with defense in depth

- **Status:** Accepted
- **Date:** 2026-09-13

## Context

Tenants are restaurants: many small customers with small data sets and a low price point. One tenant seeing or changing
another tenant's menu would be a business-ending incident, so isolation must not depend on every developer remembering a
`WHERE tenant_id = …` clause.

Options considered:

| Model | Isolation | Cost and operations |
| --- | --- | --- |
| Database per tenant | Strongest | Expensive, migrations × N, connection pool per tenant |
| Schema per tenant | Strong | Catalog bloat, migrations × N |
| **Shared schema + `tenant_id`** | Only as strong as its enforcement | Cheapest and simplest to operate |

## Decision

Use a shared schema with `tenant_id`, and enforce isolation in five independent layers.

### 1. Resolution is explicit per endpoint

Endpoints declare their tenant source as metadata: `RequireTenantFromRoute()` for the public menu (slug in the URL, reached
through QR codes) or `RequireTenantFromClaims()` for staff (the `tenant_id` claim of a tenant-scoped token). An endpoint
without a declaration is not tenant-bound. We rejected a global fallback chain (claim, then route, then header) because it
lets a request choose the source that suits it. Headers are never trusted to select a tenant.

Unknown and suspended tenants get the same 404, so a public URL does not reveal a former customer. Malformed slugs are
rejected before the cache or the database is touched.

### 2. Tenant context is write-once

The scoped `ITenantContext` can be bound once per scope. Re-binding it to a different tenant throws
`TenantIsolationViolationException`.

### 3. Reads: named query filter that fails loudly

Every `ITenantScoped` entity gets the `Tenant` query filter automatically. The filter reads the tenant when each query
runs; with no bound tenant it throws `TenantNotResolvedException`. Returning all rows, or silently returning none, would
hide bugs.

### 4. Writes: SaveChanges guard

Before saving, an interceptor checks the owner of every added, modified or deleted tenant-scoped row, using original
values, so manually attached entities are covered. A tenant-bound scope may only modify its own `Tenant` record.

### 5. Keys and foreign keys carry the tenant

Primary keys are `(tenant_id, id)`, and references between tenant-scoped tables use composite foreign keys such as
`menu_items (tenant_id, category_id) → menu_categories (tenant_id, id)`. The database rejects a cross-tenant reference
even if the application is wrong. Tenant-leading keys also make every lookup an index seek and keep `tenant_id` ready as
a partitioning or distribution key.

### 6. Row-level security in PostgreSQL

- A connection interceptor runs `set_config('app.current_tenant', …)` every time EF Core opens a pooled connection.
- Each tenant-scoped table has `ENABLE` and `FORCE ROW LEVEL SECURITY` and a `tenant_isolation` policy (`USING` and
  `WITH CHECK`). An empty setting matches no rows.
- The API connects as `armenu_app`: not a superuser, not the table owner, no `BYPASSRLS`. Migrations run as the owner.
- Startup refuses `No Reset On Close=true`: pooled connections must be reset so session state can never leak between requests.

## Consequences

- **Verified, not assumed.** Integration tests against real PostgreSQL cover each layer separately: the filter alone
  (superuser connection), row-level security alone (`IgnoreQueryFilters`, raw SQL), write guards and composite foreign keys.
  Guardrail tests fail when a new `tenant_id` table lacks an enforced policy or an entity lacks the filter.
- **One extra round trip per connection open** (`set_config`). This is negligible next to the query itself, and the
  public menu will be cached.
- **The `tenants` table is not protected by row-level security**, because resolution has to read it before a tenant is
  known. It must only hold non-sensitive data. Sensitive tenant data (billing, contacts) belongs in tenant-scoped tables.
- **Identity is split along the same line** (ADR-0006). `users` is global (one person, many tenants) and is reached only
  by id or e-mail. `tenant_memberships` and `user_sessions` are tenant-scoped with row-level security. Sign-in resolves
  the workspace from the URL first, so it never needs a cross-tenant read.
- **Data migrations** that touch tenant tables as a non-superuser owner must set `app.current_tenant` or temporarily
  `NO FORCE` row-level security inside the migration.
- **Cross-tenant operations** (platform support, analytics) will need a dedicated, audited path with a separate role.
  None exists yet, by design.
- **Tenant status changes** reach cached lookups within the cache lifetime (1 to 5 minutes). Sign-up already invalidates
  the lookup explicitly; suspension and slug changes will do the same when their commands are added.
