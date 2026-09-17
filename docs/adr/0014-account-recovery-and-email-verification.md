# ADR-0014: Password reset and e-mail verification through single-use links

- **Status:** Accepted
- **Date:** 2026-09-15

## Context

With e-mail in place (ADR-0013), two gaps of ADR-0006 could close: a person who forgets a password had no way back,
and a sign-up's address was never proven, although the platform sends invitations in its owner's name.

## Decision

### One mechanism: `UserToken`

A single-use link e-mailed to a user, with a purpose (`PasswordReset`, 1 hour; `EmailVerification`, 3 days). Like
refresh tokens and invitations: `{id}.{secret}` with 256 random bits, only the SHA-256 hash stored, compared in constant
time, in the URL fragment and POST bodies only. Users are global, so tokens have no tenant and no row-level security.

- A link works once. Sending a new link of the same purpose retires the unused ones.
- The purpose is checked with the secret: a verification link can never reset a password.
- A malformed link, an unknown token and a wrong secret get the same answer.

### Password reset

- `POST /api/v1/auth/password-reset` answers `202` whether or not the address has an account, and the e-mail is sent
  by a background sender (an in-memory channel and worker with retries). Neither the response nor its timing tells an
  attacker which addresses have accounts; the remaining difference is one indexed insert.
- `POST /api/v1/auth/password-reset/confirm` sets the password, ends a lockout, and marks the address verified (the link
  proved it). Every session started before the reset stops being refreshable, in every business: the refresh handler
  compares the session's start with `User.PasswordChangedAt`. No write reaches across tenants; access tokens already
  issued expire within 15 minutes.

### E-mail verification

- Sign-up sends a verification link; `POST /api/v1/me/email-verification` sends a new one.
- Accepting an invitation and resetting a password verify the address too: both links were delivered to it.
- The access token carries `email_verified`. The dashboard shows a reminder while it is false and renews the token when
  the tab is looked at again, so a link confirmed in another tab takes effect.
- **Inviting team members requires a verified address** (`team.email_not_verified`): the platform only sends e-mail in
  someone's name from an address shown to be theirs. Accounts that existed before verification were marked verified by
  the migration rather than locked out of a feature they had.

## Consequences

- The background sender keeps no durable outbox: a message lost to a restart costs the person a click on "send again".
  Superseded by [ADR-0019](0019-durable-email-outbox.md): account e-mails now go through a database outbox.
  Invitations stay synchronous because the owner is waiting for the answer (ADR-0013).
- One more table (`user_tokens`) and two columns on `users`; tokens cascade with their user.
- Rejected: signing the person in after a reset (the link would become a session; they sign in with the new password
  instead); revoking sessions by writing to every tenant's sessions (a cross-tenant write for something a timestamp
  solves); separate token tables per purpose (identical rules twice).
