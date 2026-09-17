# ADR-0019: Background e-mail goes through a database outbox

- **Status:** Accepted (supersedes the in-memory sender of ADR-0014)
- **Date:** 2026-09-15

## Context

Account e-mails (verification, password reset, deletion notice) were queued in memory. A restart, a deploy or a mail
server outage longer than three quick retries lost them. For a reset link that costs a click; for the notice that an
account was deleted (ADR-0017) there is nobody left to click. Worse, a message could go out for a change that then
failed to commit, or a commit could happen with no message queued if the process died in between.

## Decision

- `IEmailOutbox.Add(message, template)` adds an `email_outbox` row to the **caller's unit of work**. The message exists
  exactly when the change it announces is committed; handlers queue before saving. Account erasure queues its notice
  inside its own cross-business transaction.
- `EmailDelivery` claims the next due row with `FOR UPDATE SKIP LOCKED` and a lease, like the model processing queue:
  any number of instances deliver, no message is sent twice by two workers, and a message whose worker died is picked
  up when its lease expires.
- Retries after 10 s, 1 min, 5 min, 30 min and 2 h, so an outage of a few hours still ends in delivery. After the sixth
  failure the row is deleted and the failure logged and counted (`armenu.email.messages`, outcome `failed`).
- A delivered message is deleted at once. The outbox is not a mail log: rows hold an address and, for reset links, a
  secret, so they live for seconds when the mail server is up.
- The instance that committed a message wakes its worker immediately; other instances poll every 10 s
  (`Email:OutboxPollInterval`). `Email:OutboxWorkerEnabled` turns delivery off per instance.
- Invitations keep sending synchronously (ADR-0013): the owner is watching and gets a "send again" button.

## Consequences

- "At least once" rather than "exactly once": a worker that sends and dies before deleting the row sends it again when
  the lease expires. A duplicate reset e-mail is harmless; a lost one was not.
- A reset secret sits in the database until delivery, a few seconds normally, at most the retry schedule during an
  outage. Only its hash is kept afterwards, as before.
- Integration test hosts run no delivery worker: they share one database, and one host's worker would deliver another
  test's messages. Tests read the outbox for their own addresses; delivery is tested on a clock set in 2001, where no
  other test's messages are due.
- Rejected: a message broker (a new piece of infrastructure for a few messages a minute); keeping the in-memory channel
  with longer retries (still lost on every deploy).
