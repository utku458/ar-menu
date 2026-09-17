# ADR-0016: Guest analytics are daily counters, with nothing about the guest

- **Status:** Accepted
- **Date:** 2026-09-15

## Context

A restaurant paying for 3D models wants to know whether guests look at them: how many open the menu, which dishes they
open, how many start AR. Guests scan a QR code at a table; they did not sign up for tracking, and in Turkey (KVKK) and
the EU (GDPR) collecting identifiers or device data needs a reason and consent.

## Decision

- **Events:** `menu_viewed`, `dish_opened`, `model_viewed`, `ar_started` (the button press, since Scene Viewer and Quick
  Look report nothing back). The guest app counts each once per page load and sends them in batches of at most 20,
  after a pause or when the page is hidden, with `fetch(…, { keepalive: true })`.
- **Nothing identifies a guest:** no cookie, no local storage, no id, no user agent, no IP stored. The request carries
  only event types and dish ids; the end-to-end test asserts it has no cookie or authorization.
- **Storage is the aggregate:** `menu_daily_statistics (tenant_id, day, event, menu_item_id, count)`, incremented with
  `INSERT … ON CONFLICT DO UPDATE`, so concurrent guests add up without a read. Raw events are never written. Unknown or
  foreign dish ids count nothing. The table has row-level security like every tenant table.
- **Endpoint:** `POST /api/v1/menus/{tenant}/events`, anonymous, under the public menu rate limit.
- **Reading:** `GET /api/v1/manage/statistics?days=7|30|90` for owners and managers: totals, a series per day and the
  dishes guests open most, shown in the dashboard as stat tiles, a column chart with a table view, and a table of dishes
  with their AR rate. Days are UTC days.
- `armenu.menu.events` counts events by type for operators, without a tenant tag.

## Consequences

- The numbers are page loads, not people, and the dashboard says so. Unique visitors would need an identifier.
- Anyone can inflate a menu's counters within the rate limit; the numbers are for a restaurant's own decisions, not
  billing.
- Days follow the business's IANA time zone.

## Amendment (Phase 8): businesses have a time zone

`Tenant.TimeZone` (IANA names only, validated against the time zone database) decides which day an event is counted
on and where a statistics period ends. Sign-up takes the browser's zone as a hint and falls back to UTC for a name the
server does not know; owners and managers change it in settings (`PUT /api/v1/manage/settings/time-zone`). Existing
businesses start in UTC, the days their counters were recorded in. Changing the zone does not move counters already
recorded. The chiseled API image carries no time zone database, so the Dockerfile copies `/usr/share/zoneinfo` in.
- Rejected: a third-party analytics script (a new origin in the content security policy and data leaving the platform);
  storing raw events for later aggregation (personal data risk and unbounded growth for no current need).
