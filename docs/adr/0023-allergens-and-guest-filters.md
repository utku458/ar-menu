# ADR-0023: Allergens distinguish "none" from "not declared"; guests filter the menu on their own phone

- **Status:** Accepted
- **Date:** 2026-09-17

## Context

A guest with a nut allergy scans the QR code and wants to know which dishes are safe. EU Regulation 1169/2011
(Annex II), and the Turkish Food Codex labelling regulation that follows it, name fourteen allergens a food business
must be able to declare. Tourists also look for vegetarian, vegan and gluten-free dishes, often in a language they do
not read well, and search for a dish they know by name ("kofte" for "köfte").

Most menus will not have allergens filled in on day one. The dangerous failure is a menu that was never filled in
telling a guest a dish contains nothing.

## Decision

- **Fourteen allergens and three diets, as stable codes** (`gluten` … `molluscs`; `vegetarian`, `vegan`,
  `glutenFree`) in the domain, the API, the spreadsheet and `text[]` columns alike.
- **"Not declared" is `null`; "contains none" is `[]`.** The domain, the database, the API, the dashboard (a separate
  "I have entered the allergens" switch) and the spreadsheet (`none` against an empty cell) all keep the difference.
- **A diet may not contradict the declared allergens.** Vegetarian rules out fish, crustaceans and molluscs; vegan
  also milk and eggs; gluten-free rules out gluten. Allergens and diets are replaced together, so a correct end result
  never fails because of an order of edits. A vegan dish is stored as vegetarian too, so filtering for vegetarian
  finds it. What allergens cannot express (honey, gelatine) remains the business's responsibility.
- **Guests search and filter in the browser**, on the menu already downloaded: by words in the name or description
  (case, accents and the Turkish dotless ı ignored), by diets, and by allergens to avoid. What someone searches for or
  is allergic to never reaches the server, which fits ADR-0016 and adds no request and no cache variant.
- **Avoiding an allergen hides dishes whose allergens were not declared**, and says how many were hidden and why, with
  the advice to ask the staff. The dish sheet always states one of the three: the allergens, "none of the 14", or "not
  given — ask the staff", with "as declared by the restaurant".
- Changes are recorded in the history (ADR-0018) with "not entered" shown apart from "none".

## Consequences

- An allergen filter on a menu nobody filled in shows almost nothing, and says why. That is the intended pressure on
  businesses, and the safe side for guests.
- The filter is not kept in the address: a shared link opens the whole menu, and a guest's diet is not written into
  their browser history.
- Rejected: server-side search (a round trip and a log entry per keystroke for a menu of at most a few hundred dishes);
  free-text allergens (not filterable, not translatable); treating an empty list as unknown (then "contains none" could
  never be said); showing undeclared dishes under an allergen filter with a warning (a warning next to a dish that
  passed a "without nuts" filter is too easy to miss).
