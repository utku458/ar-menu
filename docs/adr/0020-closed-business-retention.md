# ADR-0020: A closed business's data is kept for 30 days, then deleted

- **Status:** Accepted
- **Date:** 2026-09-15

## Context

A business closes when its only owner deletes their account (ADR-0017). Its menu, photos, 3D models, statistics and
history then serve no one, and KVKK and the GDPR expect data without a purpose to be deleted. Deleting at once would
make a deletion by someone who broke into the account unrecoverable.

## Decision

- `TenantPurge` runs on one instance at a time (a PostgreSQL advisory lock, shared with asset cleanup through
  `ExclusiveMaintenanceWorker`) every 6 hours, and purges businesses closed longer than
  `Retention:ClosedBusinessRetention` (30 days).
- **Files first:** every object under the business's staging, sources and published prefixes. **Then rows**, in one
  transaction in a scope bound to the business, so row-level security checks each delete: processing jobs,
  statistics, history, dishes (soft-deleted included), categories, invitations, memberships. **Then** `PurgedAt`.
  A run that fails halfway leaves the business unmarked and the next run finishes; deleting rows first could leave
  files no one can find.
- **The tenant row stays**, with its name, slug and dates: the slug must never be claimed again, because printed QR
  codes point at it. It holds nothing about a person.
- `armenu.tenants.purged` counts purges; each is logged with the business id.

## Consequences

- Within the 30 days, an operator can restore a business by reopening it in the database and inviting its people
  again; there is no self-service for it.
- Suspended businesses are not purged: suspension is meant to be lifted.
- Backups still hold purged data until they expire; backup retention should be shorter than a year and documented with
  the hosting setup.
- Rejected: deleting at the moment of closing (no way back from a hijacked account); cascading deletes from the tenant
  row (would delete the slug reservation, and bypass the file cleanup).
