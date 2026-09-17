# ADR-0005: CQRS with source-generated Mediator

- **Status:** Accepted
- **Date:** 2026-09-14

## Context

The initial plan named MediatR for CQRS. Since version 13 (2025) MediatR is commercially licensed, and this product is
meant to be sold. Separately, we had to decide how handlers reach the database without weakening the persistence
guarantees from ADR-0003 (tenant write guard, soft delete, cache invalidation), all of which run in `SaveChanges`.

## Decision

### Mediator library

Use [Mediator](https://github.com/martinothamar/Mediator) (MIT). It has the same request/handler/pipeline model as
MediatR, but a source generator wires handlers at compile time: no reflection, no runtime scanning, and a missing
handler is a build error. Handlers are registered with a **scoped** lifetime because they depend on the scoped
`DbContext` and tenant context. The library's default is singleton.

### Writes: commands through aggregates

Command handlers live in **Application**. They load aggregates through aggregate-specific repositories (contracts in
Domain, implementations in Infrastructure), call domain behavior, and commit with `IUnitOfWork`. There is no generic
repository, and no `IQueryable` or `DbSet` reaches the application layer. Bulk operations such as `ExecuteUpdate` and
`ExecuteDelete` are therefore unavailable to use cases. That matters because they would silently bypass the tenant
write guard, soft delete and cache invalidation.

### Reads: queries as projections next to the database

Query contracts and response models live in Application. Their **handlers live in Infrastructure** and project straight
from `DbContext` into immutable DTOs. Reads never load aggregates, and query filters and row-level security still apply.
The public menu query is cached in HybridCache per tenant and language, and evicted by tenant tag from a
`SaveChanges` interceptor, so no command can forget to invalidate it.

### Pipeline

`LoggingBehavior` records the message type, outcome, error code and duration, never the message contents, which can
hold passwords and tokens. `ValidationBehavior` runs every FluentValidation validator and short-circuits into a failed
`Result` with all field errors. It creates the failure through the static abstract `IFailureFactory<TSelf>`, without
reflection or exceptions.

Validators reuse domain factories (`MustBeValid(TenantSlug.Create)`), so each rule is written once and still reported
per field with the domain's error code. Validators that need tenant settings, such as allowed languages, inject
`ITenantContext`.

### HTTP mapping

Endpoints bind request DTOs, send commands or queries, and translate results into RFC 9457 problem details:

| Error type | Status |
| --- | --- |
| `Validation` | 400, with `errors` and machine-readable `errorCodes` per field |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `Unauthorized` | 401 |

A global `IExceptionHandler` maps the few meaningful exceptions (concurrency conflict, unique-constraint race) to 409.
Everything else becomes an opaque 500 with a trace id, and a tenant isolation violation is logged as critical.

## Consequences

- Architecture tests enforce the split: command handlers only in Application, query handlers only in Infrastructure,
  messages are immutable records, and endpoints never depend on Infrastructure or EF Core.
- Two places to look for handlers. The rule is easy to state: writes are in Application, reads next to the database.
- Query handlers are tested as integration tests against PostgreSQL, which is where their correctness actually lives.
- Cache invalidation is per instance until a distributed backplane is added; the short local cache lifetime (2 minutes)
  bounds staleness across instances.
