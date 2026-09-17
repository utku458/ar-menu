# ArMenu runbook

What to do when something goes wrong, and how routine operations are done. Each alert in
[`deploy/observability/alert-rules.yml`](../../deploy/observability/alert-rules.yml) links to its section here.

Read-only SQL below runs as the schema owner (row-level security would hide other businesses' rows from the runtime
role). Queries never select e-mail addresses, message bodies or menu content: the numbers are enough to diagnose, and
personal data stays where it is.

## The system in one page

| Part | Runs as | Talks to | Health |
| --- | --- | --- | --- |
| API (`src/ArMenu.Api`) | Stateless container, any number of instances | PostgreSQL, object storage, asset processor, SMTP | `/health/live`, `/health/ready` (database) |
| Background workers | Inside every API instance | The same | Backlog gauges (below) |
| Asset processor (`web/services/asset-processor`) | Container with a headless browser | Object storage through presigned URLs only | `/health/ready` |
| Guest app, dashboard | Static files behind nginx | The API, the assets host | — |
| PostgreSQL 18 | Managed database | — | — |
| Object storage | S3-compatible, two buckets: `uploads` (private), `assets` (public, CDN) | — | — |

Workers in every API instance, and what keeps two instances from doing the same work:

| Worker | Every | Coordination | Metric that shows it stopped |
| --- | --- | --- | --- |
| E-mail delivery | 10 s, or at once when a message is queued | Row lease, `FOR UPDATE SKIP LOCKED` | `armenu.email.outbox.oldest_age` |
| Model processing | 5 s, or at once | Row lease | `armenu.ar_model.processing.queue.overdue` |
| Asset cleanup | 6 h | PostgreSQL advisory lock | `armenu.assets.cleanup.deleted` |
| Closed business purge | 6 h | PostgreSQL advisory lock | `armenu.tenants.purge.overdue` |
| Backlog sampling | 30 s | None needed (read only) | The gauges themselves; see [ArMenuMetricsMissing](#armenumetricsmissing) |

## Releasing

1. **Migrations first.** Run the migration bundle image (`target: migrations` of `src/ArMenu.Api/Dockerfile`) with the
   schema owner's connection string. It applies pending migrations in a transaction and exits non-zero on failure;
   nothing else changes when it fails.
2. **Then the API**, as a rolling update. Old and new instances run side by side for a while, so every migration must
   work with the previous release: add columns as nullable or with a default, and remove anything only in a later
   release, once no instance reads it. All migrations so far follow this.
3. **Then the web apps.** They are static; a guest with the old app open keeps working, because the API only adds
   fields.
4. **Check:** `/health/ready` on each instance, the error rate, and that a guest menu opens.

**Rolling back** means deploying the previous images. Migrations are not reversed: they only add, so the previous API
runs on the newer schema. Never run a `Down` migration in production; it drops data added since.

## Secrets

| Secret | Where | Rotating it |
| --- | --- | --- |
| `Authentication:Jwt:SigningKey` | API | Replace and restart. Access tokens (15 min) signed with the old key stop working; open dashboards get a new one through the refresh cookie, which is stored hashed in the database and does not depend on the key. |
| Runtime database password (`armenu_app`) | API | Create the new password, deploy, then remove the old one. |
| Schema owner password | Migration job only | Never given to the API. |
| `Storage:AccessKey`/`SecretKey` | API | Add a second key to the storage user, deploy, remove the first. Presigned URLs already issued (minutes) fail after removal; the dashboard asks to choose the file again. |
| `AssetProcessor:Token` = `ASSET_PROCESSOR_TOKEN` | API and processor | Both must match. Deploy the processor and the API together; jobs refused in between are retried. |
| SMTP credentials | API | Replace and restart. Messages refused meanwhile stay in the outbox and are retried. |

A secret found in a log, a ticket or a repository is rotated, not only deleted.

## Backups and personal data

- **Database:** point-in-time recovery on the managed database, at least 7 days. Restoring to a point before a
  business's purge brings its data back; purges are only final once the backups made before them have expired. Keep
  backup retention short enough that the 30-day promise in [ADR-0020](../adr/0020-closed-business-retention.md) still
  means something (recommended: 7–14 days).
- **Object storage:** published asset keys are immutable, so versioning is not needed to survive an overwrite. Bucket
  replication protects against losing the region. Deleted files (cleanup, purge) are not restorable without versioning.
- **A person asks for their data to be deleted:** they delete their account in the dashboard
  ([ADR-0017](../adr/0017-ownership-transfer-and-account-deletion.md)). Nothing needs to be done by hand.

## Alerts

### ArMenuMetricsMissing

No instance has reported `armenu_email_outbox_pending` for 15 minutes.

1. Is the API running? Check `/health/live` on each instance and the orchestrator's restarts.
2. If it runs, is telemetry reaching the collector? `OTEL_EXPORTER_OTLP_ENDPOINT` set, collector up, network open.
3. If other API metrics arrive but not the backlog: `BacklogMetrics:Enabled` may be `false`, or sampling fails. Look for
   `Sampling the operational backlog failed` in the logs; it reads `email_outbox`, `ar_model_processing_queue` and
   `tenants`.

### ArMenuServerErrors

More than 2% of requests fail with a 5xx.

1. Group the traces or logs by route and `error.type`: one route or all of them?
2. All routes and `/health/ready` failing: the database. Check connections (`max_connections`, the pool), locks and
   storage space.
3. Asset upload routes only: object storage. The API logs the S3 status code.
4. After a release: roll back ([Releasing](#releasing)), then investigate.

Problem details returned to clients never contain exception messages; the logs have them, with the trace id the client
received.

### ArMenuSlowRequests

The 95th percentile of commands and queries is above one second.

1. `armenu.use_case.duration` by `armenu.use_case`: which one is slow?
2. A public menu query that is slow means a cache miss storm; the menu cache holds entries for 10 minutes per instance.
   Many instances starting at once is expected to cause a short burst.
3. Anything else: look at the database spans of a slow trace, and at `pg_stat_statements` for the statement.

### ArMenuEmailDelayed

An e-mail has been waiting more than 30 minutes. People asking for a password reset or verifying their address are
waiting too.

```sql
SELECT template, attempts, created_at, available_at, lease_expires_at
FROM email_outbox
ORDER BY created_at
LIMIT 20;
```

- `attempts` growing, `lease_expires_at` empty: the mail server refuses the messages. See
  [ArMenuEmailsFailing](#armenuemailsfailing).
- `attempts` at 0: nothing is trying. See [ArMenuEmailOutboxStuck](#armenuemailoutboxstuck).

### ArMenuEmailOutboxStuck

A message is older than every retry allows (retries stop 2 h 36 min after it was queued): no worker is delivering.

1. `Email:OutboxWorkerEnabled` must be `true` on at least one instance.
2. Look for `Looking for e-mails to deliver failed` (the worker cannot read the outbox) or `E-mail delivery through … failed`
   in the logs.
3. A `lease_expires_at` in the future on the oldest row means a worker holds it. Leases expire after `Email:OutboxLease`
   (2 minutes) and the message becomes available to every instance again, so a row that stays claimed means workers keep
   claiming it and dying before they finish. Restart the instances and read their last logs; `Email:Timeout` bounds the
   SMTP connection.

Messages are delivered at least once: a worker that dies after sending but before deleting the row sends the message
again when the lease expires.

### ArMenuEmailsFailing

More than one e-mail in five is refused.

1. `E-mail delivery through … failed` carries the SMTP status. `535` is credentials, `550`/`554` a sender or content refusal, a timeout the network.
2. Credentials or sender domain (SPF, DKIM, DMARC) changed recently? The provider's dashboard shows refusals too.
3. `Gave up delivering a … e-mail` marks each message that ran out of retries; those are gone. The people concerned request a new link; invitations can be sent again
   from the team page.

### ArMenuModelProcessingOverdue

A 3D model has been due for more than 10 minutes. The business sees "processing" and keeps its previous model meanwhile.

```sql
-- status: 1 queued, 2 processing, 3 succeeded, 4 failed, 5 superseded
SELECT q.available_at, q.lease_expires_at, p.status, p.attempts, p.failure_code
FROM ar_model_processing_queue q
JOIN ar_model_processings p ON p.id = q.processing_id
ORDER BY q.available_at
LIMIT 20;
```

1. `lease_expires_at` in the future: a worker is on it. Processing takes up to `AssetProcessor:Timeout` (5 minutes).
2. No lease: no worker picks it up. `AssetProcessor:WorkersEnabled` must be `true` somewhere.
3. The processor's `/health/ready`; `processor.busy` (503) in the API logs means its concurrency is exhausted: scale the
   processor, not the API's `AssetProcessor:Workers`.

### ArMenuModelProcessingFailing

More than a quarter of attempts end in `retrying` or `failed`. Rejections of broken files are not counted: they are the
file's fault.

1. `armenu.ar_model.processing.attempts` by `error.type`.
2. Timeouts: large models or a slow processor. Raise `AssetProcessor:Timeout` (and keep `Lease` above it) or scale.
3. `asset.processing_output_invalid`: the processor uploaded something the API does not accept. A processor release is
   the usual cause; roll it back.
4. Storage errors from the processor: the presigned URLs point at `Storage:InternalServiceUrl`; check that the processor
   reaches it and that `ASSET_PROCESSOR_STORAGE_ORIGINS` allows it.

### ArMenuPurgeOverdue

A closed business is kept more than a day beyond its 30 days. Its owner was promised deletion.

1. `Retention:Enabled` must be `true` on at least one instance.
2. Look for `Closed business purge failed; it will run again at the next interval` in the logs. A purge deletes files first, then rows; a failure leaves the
   business unmarked and the next run starts over, so a persistent failure repeats every 6 hours.
3. An advisory lock held by a hung instance blocks every other instance: restart it.

```sql
SELECT id, closed_at, purged_at FROM tenants WHERE status = 'Closed' AND purged_at IS NULL ORDER BY closed_at;
```

## Routine tasks

- **A business wants its closed account back** within 30 days: not supported by the product. Its data is still there; a
  developer can set `status` back after confirming the request through a channel other than e-mail (the account that
  closed it was deleted).
- **A slug is taken by a closed business:** by design; printed QR codes still point to it
  ([ADR-0020](../adr/0020-closed-business-retention.md)).
- **Reprocess every model** after a pipeline improvement: sources are kept while their model is in use. There is no bulk
  command yet; businesses can upload again.
- **Disk on the database:** `menu_daily_statistics` grows by at most one row per dish per day; `audit_log_entries` by
  edits. Neither is purged except with its business.
