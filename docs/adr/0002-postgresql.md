# ADR-0002: PostgreSQL instead of MySQL

- **Status:** Accepted
- **Date:** 2026-09-13

## Context

The initial plan suggested MySQL. The system has three demanding requirements: tenant isolation that must hold even when
application code is wrong, customer-facing content in several languages, and staying on the current EF Core release.

## Decision

Use PostgreSQL 18 through the Npgsql EF Core provider.

1. **Provider parity.** Npgsql's provider ships with every EF Core release (10.0.x is available). The widely used MySQL
   provider, Pomelo, is still at 9.0.0 and does not support EF Core 10. Oracle's MySQL provider lags behind in features.
2. **Row-level security.** PostgreSQL can enforce tenant isolation inside the database (see ADR-0003). MySQL has no equivalent.
3. **Data types that fit the model.** `jsonb` for localized texts, native arrays for supported cultures, a native 16-byte
   `uuid` for UUIDv7 keys, and the `xmin` system column as a free optimistic-concurrency token.
4. **Hosting options.** Managed PostgreSQL with generous free tiers (Neon, Supabase) as well as AWS, Azure and GCP offerings.

## Consequences

- Row-level security policies are PostgreSQL-specific SQL in migrations. Portability to other databases is not a goal.
- The team needs to understand PostgreSQL roles, row-level security and connection pooling behavior.
- Local development and tests use the official `postgres:18-alpine` image (docker compose and Testcontainers).
