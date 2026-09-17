# ADR-0018: A business's history is written by the save pipeline and names people only by account

- **Status:** Accepted
- **Date:** 2026-09-15

## Context

Several people now edit one menu (ADR-0013). When a price is wrong or a dish disappears, owners ask who did it and
when. The answer must be complete (no command may forget to record), must belong to the business like the rest of its
data, and must not become a store of personal data that outlives an erased account (ADR-0017).

## Decision

- **Where:** `AuditTrailSaveChangesInterceptor` reads the change tracker before every save and adds `audit_log_entries`
  in the same transaction. An entry exists exactly when its change was committed; a failed save leaves none behind.
  It runs first, while deletions are still deletions (soft deletion rewrites them afterwards).
- **What:** menu items (name, description, price, category, photo, 3D model, visibility, availability), categories
  (name, description, visibility), business settings (name, languages, time zone) and team members (joined, role,
  removed or left). Only these fields, with before and after values as JSON; timestamps and internal keys are not
  history. A file shows as present or absent, never by its storage key.
- **Reordering** changes many rows for one intention, so it becomes one `reordered` entry per list.
- **Who:** the account of the request's access token (`ICurrentUser`); `null` means the system (model processing).
  Someone accepting an invitation acts for themselves.
- **People are ids only.** Names are joined at read time, so an erased account reads as "deleted account" everywhere
  at once. Menu subjects keep their name at the time, since a deleted dish has no current name.
- **Reading:** `GET /api/v1/manage/history?before=&limit=` for owners and managers, newest first, paged by the entry id
  (a version 7 UUID, ordered by time) instead of offsets, so new entries never shift pages. Row-level security applies.
- The dashboard's history page turns entries into sentences in Turkish or English, grouped by the business's days.

## Consequences

- Changes made with raw SQL bypass the interceptor; the only such write, account erasure, records departures itself.
- Entries are kept for the life of the business. They hold no names or addresses, so erasure needs no rewrite.
- Rejected: logging from each command handler (easy to forget, and an entry could exist for a change that rolled back);
  PostgreSQL triggers (no knowledge of the acting person without passing it through session settings, and business
  meaning such as reordering is lost); storing actor names (personal data that erasure would have to hunt down).
