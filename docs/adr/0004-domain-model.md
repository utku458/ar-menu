# ADR-0004: Domain model

- **Status:** Accepted
- **Date:** 2026-09-13

## Decisions

### Small aggregates referencing each other by id

`Tenant`, `MenuCategory` and `MenuItem` are separate aggregates. A menu item references its category by id, so two staff
members editing different items never conflict, and changing one price never loads a whole menu. Cross-aggregate rules
(the category exists in the same tenant, the price uses the tenant currency) belong to the application layer and are
backed by database constraints.

### Strongly-typed UUIDv7 identifiers

`TenantId`, `MenuCategoryId` and `MenuItemId` wrap a `Guid`, so passing one where another is expected does not compile.
In a multi-tenant system that removes a whole class of data-leak bugs. UUIDv7 values are time-ordered, which is friendly
to B-tree indexes, and unlike integer keys they reveal no business volume and cannot be guessed sequentially.

### Value objects for every rule-bearing concept

| Value object | Rule it owns |
| --- | --- |
| `LocalizedText` | Customer-facing text in several languages (stored as `jsonb`); resolves requested → neutral parent → tenant default → any. Multi-language from day one because tourist markets need it. |
| `CultureCode` | Normalized `language[-script][-region]` tag |
| `Money` / `Currency` | Non-negative, fits `numeric(12,2)`, **refuses** to round rather than silently changing a price |
| `TenantSlug` | DNS-label syntax (so it can become a subdomain), reserved platform names, immutable because printed QR codes depend on it |
| `AssetPath` | Relative storage key, never a URL. Storage or CDN providers can change without a data migration, and delivery can use immutable, long-cached URLs. |
| `ArModel` | A `.glb` model (web, Android, WebXR), an optional `.usdz` (iOS Quick Look) and an optional poster image shown instantly while the model streams |

### Result pattern for expected failures

Domain operations that can fail for business reasons return `Result`/`Result<T>` with a typed `Error` and a stable code
such as `tenant.slug_reserved`. Exceptions are reserved for programming errors (for example a default identifier). Failure
is part of the method signature, and errors map cleanly to problem details.

### Persistence ignorance

The domain has no EF Core attributes or references. Mapping lives in Fluent API configurations and model-wide conventions.
EF Core uses private parameterless constructors for materialization. Value converters go through the same validating
factories, so an invalid row fails loudly instead of creating an invalid object.

### Cross-cutting state handled by infrastructure

- **Soft delete** for menu content (`ISoftDeletable`): a delete becomes an update, keeping restore and history possible.
- **Audit timestamps** (`IAuditable`) come from `TimeProvider`, so they are testable.
- **Optimistic concurrency** uses PostgreSQL's `xmin` as a shadow property, without polluting the domain.

### Aggregate-specific repositories, no generic repository

`DbContext` already implements unit of work and repository. A generic `IRepository<T>` either hides EF Core's query power
or leaks `IQueryable`. Instead, each aggregate has a small repository contract in the domain, shaped by the use cases
that need it (e.g. `IMenuItemRepository.GetNextDisplayOrderAsync`), plus `IUnitOfWork`. Reads bypass repositories
entirely; see ADR-0005.

## Consequences

- More types than a CRUD model, but every rule has exactly one home and is unit-tested without a database.
- Domain events (`TenantCreated`, `TenantSuspended`, `TenantReactivated`, `RefreshTokenReuseDetected`) are raised but not
  dispatched yet. Dispatching through an outbox arrives with the first consumer (e-mail notifications).
