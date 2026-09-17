# ADR-0021: Menus are edited in bulk as a CSV spreadsheet, previewed and applied all or nothing

- **Status:** Accepted
- **Date:** 2026-09-15

## Context

Restaurants change prices all at once (inflation in Turkey makes this frequent) and translate a whole menu at a time.
Doing it dish by dish in the editor takes an hour. They already know spreadsheets.

## Decision

- **Export** (`GET /api/v1/manage/menu/export`, owners and managers): UTF-8 CSV with a byte order mark, one row per dish
  in menu order, columns `id, category, name:{language}…, description:{language}…, price, visible, available`, the
  default language first. Prices use a decimal point; flags are `yes`/`no`.
- **Import** (`POST /api/v1/manage/menu/import?dryRun=`, `text/csv`, at most 1 MiB and 2,000 rows):
  - A row with an id updates that dish; an empty id adds a dish to the category named in the default language.
    Dishes missing from the file are **left alone**: a file never deletes.
  - Only the columns present change anything, so a file with just `id,name:tr,price` updates prices.
  - Read as spreadsheets in Turkey save: semicolon or comma separators, `185,50` or `185.50`, `evet`/`hayır`. Prices with
    thousands separators or three decimals are refused, because `1.250` is ambiguous.
  - **All or nothing.** Every problem is reported with its line and column, using the domain's error codes where the rule
    lives there (a missing default-language name, a price too large). One problem, no change.
  - **Dry run first.** The dashboard uploads the file as a dry run, shows the problems or the dishes and columns that
    would change, and applies the same file on confirmation. Both go through the same code on the tracked menu; only
    the save differs, so the preview cannot disagree with the result.
- Changes go through the domain methods and the normal save, so the history (ADR-0018) records each changed dish.
- **Formula injection:** cells starting with `=`, `+`, `-` or `@` are exported behind an apostrophe, which spreadsheets
  show as text, and the apostrophe is removed on import.

## Consequences

- No optimistic concurrency across the round trip: a price changed in the editor between export and import is
  overwritten by the file. The preview shows it among the changes.
- Excel in a Turkish locale opens comma-separated files in one column; the dashboard tells people to save as CSV, and
  Google Sheets or Excel's import handle the export. Semicolon files are accepted back.
- Rejected: `.xlsx` (a large dependency and a format with macros and formulas to sanitize); applying valid rows and
  skipping invalid ones (a half-applied price list is worse than none); deleting dishes missing from the file (a filtered
  or truncated spreadsheet would wipe the menu).
