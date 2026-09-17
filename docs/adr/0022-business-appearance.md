# ADR-0022: A business's colour fills shapes on its menu; the API decides the text drawn on it

- **Status:** Accepted
- **Date:** 2026-09-16

## Context

Until now every guest menu looked the same. A restaurant that prints its logo on the door and its colour on the awning
arrives at a menu that shows neither, and the QR code leads somewhere that does not look like the place the guest is
sitting in.

Letting a business choose a colour is where this usually goes wrong. A colour picked for a sign is picked against
white card, in daylight; the menu shows it on a cream page in light mode and on a near-black page in dark mode, next to
text. A dark green that reads well on paper disappears as text on a dark menu; a bright yellow disappears on a light
one. Nothing stops a business from picking either.

## Decision

- **What a business sets** (`PUT /api/v1/manage/settings/branding`, owners and managers): its name, a logo and one
  colour, saved together. `null` means "no logo" and "no colour"; the whole appearance is replaced on every save, so
  removing either is an ordinary save and the history (ADR-0018) records one entry for the change.
- **The colour is `#rrggbb` and nothing else.** No transparency, no CSS colour function, no colour name, no shorthand.
  The value is interpolated into a page; six hexadecimal digits cannot carry markup or a CSS expression, and an opaque
  colour cannot vanish into the page behind it.
- **The colour only ever fills a shape** — the band at the top of the menu, the table chip, the AR button — and never
  writes text on the menu's own background. That is the only use a business's colour can be trusted with: the pairing
  of a fill and the text on it is checked, while the same colour as text on a page whose background changes with the
  guest's colour scheme is not.
- **The API decides the text on the fill.** `GET /api/v1/menus/{slug}` returns `accentColor` with `onAccentColor`
  beside it, chosen as whichever of white and black contrasts more with the colour. The rule is written once, in the
  domain, and unit-tested; the guest app sets both as CSS custom properties on the document element, so it never
  computes a colour of its own and the dashboard cannot disagree with the menu.
- **Black, not the menu's near-black.** Only the two extremes are far enough apart that *every* colour reaches WCAG's
  4.5:1. A mid grey — the hardest case — reaches it with neither white nor a softened near-black.
- **The logo goes through the existing upload path** as a new asset kind: a presigned upload straight to storage, then
  publication under an immutable key after the API reads the file's own bytes. WebP, AVIF, PNG and JPEG only, at most
  512 KB. **SVG is refused**: it is a document that can carry script, and it would be served from the assets host.
- The logo is stored as a storage key, like every other asset, and resolved to a CDN URL at read time.

## Consequences

- The appearance lives on the tenant row, which the guest menu is already served from (`TenantInfo`), so a menu with a
  logo and a colour costs no extra query and the existing cache invalidation covers it.
- Asset cleanup had to learn about the logo: it is the first published file no menu item points at, so without it the
  logo would be taken for an orphan and deleted once the grace period passed. An integration test pins this.
- Purging a closed business (ADR-0020) clears the appearance, because the file it named was among the deleted ones.
- The rest of the menu keeps the platform's palette, which is contrast-checked in both colour schemes. A business's
  colour therefore cannot make its own menu unreadable — and cannot restyle the menu completely either. That is the
  trade accepted here.
- Rejected: deriving a readable text colour from the business's colour by lightening or darkening it (no mix ratio
  guarantees contrast for every colour, and the guest app would be recomputing a rule the API already owns); returning
  a separate accent per colour scheme (the API would have to know the web apps' palettes); a full theme editor
  (several colours multiply the ways a menu can end up unreadable, for little more recognition than a logo and one
  colour give).
