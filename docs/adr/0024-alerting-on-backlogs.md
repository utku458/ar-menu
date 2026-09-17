# ADR-0024: Alerts watch what is waiting, and every alert has a runbook section

- **Status:** Accepted
- **Date:** 2026-09-18

## Context

Everything that matters most to a business happens in a background worker inside the API: the password reset e-mail,
the 3D model, the deletion of a closed business's data. Counters say what those workers did. Nothing said what they
stopped doing. A mail worker that dies simply stops incrementing `armenu.email.messages`, and a metric that stops
moving looks exactly like a quiet night.

Error-rate alerts have the same blind spot: a queue that nobody reads produces no errors at all.

## Decision

- **Five gauges measure what is waiting**, not what happened: e-mails in the outbox and the age of the oldest one,
  processings queued and how overdue the most overdue one is, and closed businesses kept past their retention period.
  Each is the one number that goes up when the matching worker stops.
- **A worker samples them on a timer** (30 s, `BacklogMetrics:Interval`) and the gauges report the last sample. Reading
  the database inside the exporter's callback would turn a collector's scrape interval into database load and let a slow
  query stall an export. Nothing is reported before the first sample: a zero would claim a backlog nobody measured.
- **Every instance samples the same database**, so alert rules take `max()`, never `sum()`.
- **Alert rules live in the repository** ([`deploy/observability/alert-rules.yml`](../../deploy/observability/alert-rules.yml))
  in Prometheus' format, which Grafana Mimir, Thanos and Alertmanager-compatible stacks also read.
- **The rules are tested.** `alert-rules.test.yml` plays series through them and asserts what fires and when; CI runs
  `promtool check rules` and `promtool test rules`. A threshold that silently stops firing is a page nobody gets.
- **Every alert links to a section of the [runbook](../operations/runbook.md)** carrying the diagnosis, the SQL to run
  and the fix. An alert nobody knows how to answer is noise.

## Consequences

- The database gets five small aggregate queries per instance every 30 seconds. On the tables they read (outbox, queue,
  tenants) that is a few milliseconds; the interval is configurable, and sampling can be turned off entirely.
- A database that cannot be read leaves the gauges at their last values, which could hide a growing backlog. Losing the
  database is the readiness probe's and the server-error alert's business, and `ArMenuMetricsMissing` catches an API
  that is gone altogether.
- The alerts describe symptoms a guest or an owner would notice ("password resets are not arriving"), not internals, so
  the severity is about people rather than processes.
- Rejected: alerting on counter rates alone (silent when a worker dies); reading the backlog inside the metrics callback
  (scrape-driven database load); keeping rules in the monitoring tool's UI (untested, unreviewed, lost on rebuild).
