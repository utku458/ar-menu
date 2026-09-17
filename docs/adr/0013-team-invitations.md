# ADR-0013: Team members join by e-mail invitation

- **Status:** Accepted
- **Date:** 2026-09-15

## Context

Only sign-up created accounts, so a business had exactly one person: its owner. Restaurants need managers who edit the
menu and staff who mark dishes as sold out (the roles have existed since ADR-0006). Adding people raises the usual
questions: how does the system know the person controls the address, what if they already have an account (users are
global, one identity across businesses), and how does access end?

## Decision

### The flow

1. The **owner** invites an address as manager or staff (`POST /api/v1/manage/team/invitations`). Ownership is not
   handed over or shared this way.
2. The API saves the invitation, then e-mails a link: `{dashboard}/{workspace}/join#{invitationId}.{secret}`.
3. The dashboard reads the secret from the fragment, removes it from the address bar, and asks what the invitation
   offers (`POST …/tenants/{workspace}/invitations/lookup`): the business, the role, the address, and whether the
   address has an account.
4. The person joins (`POST …/invitations/accept`) and is signed in with the usual session cookie:
   - **No account:** they choose a name and a password. Receiving the link proves they control the address.
   - **An account:** they enter its password, with the same verification, lockout and identical failures as signing
     in, so a link cannot be used to guess passwords.

### The secret

The same format as refresh tokens (`OpaqueToken`): 256 random bits, only a SHA-256 hash stored, compared in constant
time, a dedicated hash type so it can never be checked against a session. It travels in the URL fragment (never sent
to servers, absent from access logs and `Referer`) and in POST bodies, never in a path or query string. The lookup and
accept endpoints are rate limited like authentication and scoped to the workspace in the URL, so another business's
invitation does not exist there. A malformed link, an unknown invitation and a wrong secret get the same answer.

### Rules

- One pending invitation per address per business, enforced by a partial unique index. An expired one is replaced
  when the address is invited again.
- Invitations expire after 7 days. Sending again issues a new link and deadline, and the old link stops working, which
  also fixes a link sent to the wrong inbox.
- The owner changes members between manager and staff and removes them; the owner's own role and membership cannot be
  changed. A role change applies from the member's next access token; removing a member deletes their sessions
  (cascade), so they cannot refresh.
- Invitations are tenant-scoped rows with row-level security, like every business-owned table.

### E-mail

`IEmailSender` in the application, MailKit over SMTP in infrastructure (Microsoft advises against `System.Net.Mail`
for new code), Mailpit locally. Messages are Turkish or English (the inviting owner's interface language), plain text
and HTML, with every business-provided value HTML-encoded.

The e-mail is sent after the invitation is committed. If the mail server refuses, the invitation stays and the response
says `emailSent: false`; the dashboard offers to send it again. Delivery failures are logged (server and reason, not the
address) and counted in `armenu.email.messages`.

## Consequences

- The e-mail itself is the proof of control over the address; there is no separate verification step.
- A failed delivery needs one click from the owner instead of a retry the system does on its own. An outbox with a
  worker would retry automatically, at the cost of a second queue; with one e-mail type sent by a person watching the
  screen, the explicit answer is the better trade. When transactional e-mail grows (password reset, receipts), an
  outbox table processed like the model queue is the next step.
- A removed member's current access token works until it expires (at most 15 minutes, ADR-0006).
- Integration tests cover the whole flow with a fake sender, and the real SMTP sender against a Mailpit container: the
  link in the message that arrives is the one that joins the team.
- Rejected: shareable join codes (no proof of the address); tokens in the query string (logs, history, `Referer`);
  magic links that sign existing users in without their password (a leaked invitation would become account access);
  letting managers invite (they could create accounts with more rights than intended as the product grows).
