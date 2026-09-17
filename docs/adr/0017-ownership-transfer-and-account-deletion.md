# ADR-0017: Ownership is handed over, and deleting an account erases the person but not the businesses they worked in

- **Status:** Accepted
- **Date:** 2026-09-15

## Context

A business had one owner forever, and nobody could delete their account. Both are gaps: owners sell restaurants and
change jobs, and KVKK and the GDPR give people the right to have their personal data erased. Accounts are global
(ADR-0006), so deleting one reaches into every business the person belongs to, which no tenant-bound request can do.

## Decision

### Handing ownership over

- `POST /api/v1/manage/team/members/{id}/ownership` with the owner's password. `TenantMembership.HandOverOwnershipTo`
  makes the member the owner and the former owner a manager in one save, so a business never has zero or two owners.
- The role in the database decides, not the one in the access token, which may be minutes old.
- **Password confirmation** (`PasswordConfirmation`, also used for deletion): a stolen token or an unlocked laptop is not
  enough. Wrong passwords count towards the sign-in lockout, and are answered as a field error (400), not 401, which
  would end the session the person is using. The dashboard renews its token right after, so owner pages disappear.

### Deleting an account

- `GET /api/v1/me/deletion` tells what would happen; `POST /api/v1/me/deletion` does it, with the password.
- **Refused while the person owns a business that has other members:** those people depend on it, so it must be handed
  over first. A business only the person could run is **closed** (`TenantStatus.Closed`, final): its menu and sign-in
  stop, and its slug is never reused, because printed QR codes would otherwise open someone else's menu.
- **Erasure, not deletion, of the user row.** `User.Erase` replaces the address with `erased-{id}@erased.invalid`, the
  name with nothing and the password hash with a value no password matches, and outdates every session. The row stays
  so a business's history (ADR-0018) keeps pointing at "a deleted account". The address is free for a new account.
- Memberships are deleted in every business (sessions go with them by cascade), user tokens are deleted, and each
  business's history records that a member left.
- A goodbye e-mail goes to the former address, so a deletion by someone who got into the account does not go unnoticed.

### Writing across businesses

`AccountErasure` works in a scope bound to no tenant, in **one transaction**, and names each business in turn with a
transaction-local `app.current_tenant`. Row-level security still checks every read and write against the business it
concerns; nothing gains a bypass. Memberships are listed through the `own_memberships` policy (ADR-0015). If someone
joins a business between the preview and the deletion, the transaction notices and changes nothing (409).

## Consequences

- A closed business's menu data and files are kept for 30 days, then deleted ([ADR-0020](0020-closed-business-retention.md)).
- A deleted person's access token for one business stays valid until it expires (at most 15 minutes), like a removed
  member's (ADR-0013); every refresh fails at once.
- Rejected: hard-deleting the user row (breaks history and foreign keys, or forces deleting history); letting deletion
  close businesses that have a team (other people lose their workplace without being asked); a `BYPASSRLS` maintenance
  role for erasure (a standing hole in isolation for one rare operation).
