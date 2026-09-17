# ADR-0007: Guest web app: a static React app with WebAR on demand

- **Status:** Accepted
- **Date:** 2026-09-14

## Context

Guests open a menu by scanning a QR code with their phone, often on a congested network in a busy restaurant, and
install nothing. A readable menu must appear fast. 3D and augmented reality set the product apart, but the 3D stack
alone (`<model-viewer>` with three.js) is about 290 KB compressed, more than twice the rest of the app. Menus depend on
the tenant and the language, and the API already caches them (ADR-0005).

## Decision

### A static single-page app on a CDN, not server rendering

| Option | For | Against |
| --- | --- | --- |
| Server rendering (Next.js, React Router framework mode) | Earliest first paint; link previews | A Node.js runtime to operate next to .NET; every QR scan costs server time |
| **Static app on a CDN** | Scales with scans at CDN cost; one deployable per app; data already cached by the API | JavaScript must run before content shows; no link previews |

The weakness of the static app, content waiting for JavaScript, is addressed directly (see First load) and measured.
Menus live at `/m/{slug}`, the URL format printed in QR codes (`TenantSlug`), so the app builds with `base: '/m/'`.

### Stack

| Concern | Choice | Reason |
| --- | --- | --- |
| UI | React 19 with React Compiler | Memoization without hand-written `useMemo`/`useCallback` |
| Build | Vite 8 (Rolldown) | Fast builds; plugin hooks used to guard the first load |
| Routing | TanStack Router | Type-checked search parameters (`lang`, `item`), shared with the upcoming dashboard |
| Data | TanStack Query | Caching per language, retries on flaky networks, refetch when a guest returns to the tab |
| Styling | Tailwind CSS 4 | CSS-first design tokens; dark mode by swapping tokens, not by duplicating classes |
| Dialogs | Native `<dialog>` | Focus trap, Escape, inert background and top layer from the browser, with no library |
| Fonts | System fonts | No font download, and native coverage of Latin, Cyrillic, Arabic and CJK scripts |

### First load

- **The menu request starts before the app code has downloaded.** The entry chunk is tiny (4 KB): it requests the
  menu and imports the application, whose chunks a build plugin preloads from `index.html`. The app takes over the
  pending response instead of asking again.
- **Measured** with Lighthouse under DevTools throttling (150 ms RTT, 1.6 Mbps, 4× CPU), median of three runs: LCP
  2,613 ms without the early request and 2,044 ms with it (22 % faster).
- **A budget:** the build fails when JavaScript loaded on startup exceeds 130 KiB gzip (116 KiB today).
- Connections to the API and the asset CDN open while the HTML parses (`preconnect`). The menu request is a plain
  `GET` without custom headers, so it needs no CORS preflight round trip.
- Nothing shifts: image sizes are declared and the loading skeleton has the shape of a menu (CLS 0).

### WebAR on demand

- `@armenu/ar-viewer` wraps `<model-viewer>`. Its chunk loads only when a guest opens a dish that has a model, and
  starts downloading earlier when a guest hovers, focuses or touches such a dish.
- **Guarded twice:** the build fails if `@google/model-viewer` or `three` end up in startup chunks, and an
  end-to-end test checks that nothing 3D is requested before a dish is opened.
- **No visible loading step:** the poster in the dish sheet is already cached from the menu list. Posters are
  rendered by `<model-viewer>` itself with the same camera and lighting preset as the in-page viewer, so the model
  replaces its poster without a jump.
- **AR modes:** WebXR, Android Scene Viewer and iOS Quick Look. `ar-scale="fixed"` shows the real portion size on
  the table; letting guests scale the dish would defeat the purpose. Desktops, which cannot start AR, show a hint to
  open the menu on a phone.
- **Data saving:** with Save-Data or `prefers-reduced-data`, models load only when the guest asks for them.
- **No third-party requests:** the demo models use `KHR_mesh_quantization`, which renderers decode natively, so no
  decoder is fetched from a public CDN. End-to-end tests fail on any request outside the app, the API and the CDN.

### The URL is the state

`/m/{slug}?lang=de&item={id}`: the language and the open dish live in the URL. The back button closes a dish sheet
instead of leaving the menu, and a link can open a dish directly. A chosen language is also remembered across
restaurants: a guest who reads German at one table wants German at the next.

### Localization

Menu content arrives translated from the API. Interface text exists in Turkish, English, German, Russian and Arabic,
with an English fallback, and always follows the language of the menu, so a page never mixes languages. The document
gets `lang` and `dir`; layout uses logical CSS properties so Arabic reads right to left; language names are shown in
their own language (`Intl.DisplayNames`); prices use the local currency symbol (`Intl.NumberFormat`).

### Testing

| Level | Tool | Covers |
| --- | --- | --- |
| Unit | Vitest | Language fallback, text direction, prices, menu request resolution |
| End to end | Playwright on the production build, phone and desktop profiles | Early menu request, lazy 3D, back button, Escape, dish links, AR availability, errors, RTL |
| Accessibility | axe-core (WCAG 2.2 AA) | Menu and open dish sheet in light and dark themes, and an RTL menu |

End-to-end tests stub the API with fixtures typed by the generated contract (ADR-0008) and serve the real demo models,
so `<model-viewer>` genuinely downloads and renders them.

## Consequences

- Lighthouse, mobile profile: Performance 97–99, Accessibility 100, Best Practices 100, SEO 100.
- Shared menu links get no rich previews, and menu content is not indexed by crawlers that do not run JavaScript.
  Edge-rendered meta tags can add previews later without server rendering the app.
- The 3D chunk is large by nature. Its cost is paid only by guests who open a 3D dish, and it is cached afterwards.
- Demo asset keys are not content-hashed, so the local CDN caches them for an hour. Uploaded assets will get
  immutable, versioned keys with the asset pipeline.
- TypeScript stays on 6.0 until typescript-eslint supports TypeScript 7.
