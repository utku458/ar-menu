# ADR-0012: Production delivery: container images, migration bundles, one security policy, OpenTelemetry

- **Status:** Accepted
- **Date:** 2026-09-15

## Context

Five phases produced an API, two web apps and a processing service that ran on a developer's machine. Shipping them
raises questions the code had so far answered with comments ("configure forwarded headers at deployment"):

- What exactly is deployed, and how does the schema change without the API running DDL as a side effect?
- Behind a load balancer every request comes from the balancer. Rate limits keyed on the client address would throttle
  all guests of a restaurant together, and trusting `X-Forwarded-For` from anyone would let attackers pick their own
  address.
- The guest app loads a WebAssembly decoder and blob URLs for 3D models. A content security policy written separately
  for the server would drift from what the bundle does, and nobody would notice until a phone showed a blank model.
- When a model takes two minutes to go live, where is the time spent?

No cloud provider has been chosen, so the answers must not depend on one.

## Decision

### Artifacts CI builds

| Image | Contents |
| --- | --- |
| `armenu-api` | `aspnet:10.0-noble-chiseled`: no shell or package manager, non-root, port 8080 |
| `armenu-migrations` | The EF Core migration bundle on `runtime:10.0-noble-chiseled`, run as the schema owner before the API rolls out |
| `armenu-asset-processor` | Playwright's image with Chrome (ADR-0011) |
| `armenu-guest` · `armenu-dashboard` | Static builds behind `nginx-unprivileged`, with the security headers below |

Web images are built per environment: `VITE_*` origins are compiled into the bundle and into its policy. Deploying the
same files to a CDN is equally valid; the headers then come from the same generator.

`deploy/compose.production.yml` runs all of them together, the API in the `Production` environment, with buckets and
CORS provisioned by a separate step (as infrastructure would), telemetry going to a collector and e-mail to Mailpit. It
is how a release is tried end to end on one machine.

### Storage endpoints

`Storage:ServiceUrl` is where the API calls storage, `Storage:PublicServiceUrl` the host browser uploads are signed for,
`Storage:InternalServiceUrl` the host processor URLs are signed for. Presigning needs no connection, so a private
network endpoint for the API and a public one for browsers is a configuration, not a code change.

### Trusted proxies

`ReverseProxy:KnownProxies` and `ReverseProxy:KnownNetworks` list the proxies whose `X-Forwarded-For` and
`X-Forwarded-Proto` are believed (loopback always), with `ForwardLimit` for chains such as CDN → load balancer.
Forwarded headers are processed first, so rate limiting, HSTS and `Secure` cookies see the real client. Integration tests
prove both halves: clients behind a trusted proxy are limited separately, and a made-up address from anyone else changes
nothing.

API responses carry `X-Content-Type-Options: nosniff`, `Referrer-Policy: no-referrer` and
`Content-Security-Policy: default-src 'none'; frame-ancestors 'none'` (JSON is never meant to render); HSTS outside
Development.

### One security policy for the web apps, tested by every end-to-end test

`web/packages/web-security` derives each app's headers from the build variables:

- **Content security policy.** `script-src 'self' 'wasm-unsafe-eval' blob:` (Meshopt's WebAssembly decoder and the empty
  in-memory script that enables it); `connect-src` and `img-src` limited to the app itself, its API and its storage
  origins; no `unsafe-eval`, no framing, no plugins, no foreign `<base>`.
- **Permissions policy.** WebXR (`xr-spatial-tracking=(self)`) and nothing else powerful.
- **`Cross-Origin-Opener-Policy: same-origin`, `nosniff`**, and a referrer policy per app.

`vite preview` serves them, so both end-to-end suites run under the production policy, and a fixture fails any test
that triggers a violation or loads a page without the policy. Removing `'wasm-unsafe-eval'` fails the 3D model tests.
The nginx images print the same headers with `nginx-headers.ts`. Hashed files are cached for a year (not their 404s),
pages are revalidated.

### Observability

OpenTelemetry in the API, configured by the standard `OTEL_*` variables and exported over OTLP when
`OTEL_EXPORTER_OTLP_ENDPOINT` is set:

- **Traces:** ASP.NET Core, HttpClient, Npgsql, and spans of our own for every use case (`TelemetryBehavior`), each model
  processing run, asset cleanup and e-mail delivery. Health checks are not traced; a sampler drops the client spans
  that background queue polls would start every few seconds.
- **Metrics:** HTTP, runtime, rate limiter and database metrics, plus `armenu.use_case.duration` (by use case, outcome
  and error code), `armenu.ar_model.processing.attempts` and `.duration` (by outcome), `.time_to_publish`,
  `armenu.assets.cleanup.deleted` and `armenu.email.messages`.
- **Logs:** exported with their trace context. The processor logs the `traceparent` it receives, so its JSON lines join
  the API's trace of the same processing.

Application code records through `System.Diagnostics` only; the exporter is a composition-root concern. Locally, the
Aspire Dashboard (a compose profile) shows all three.

### CI

GitHub Actions runs on every pull request: `dotnet format`, build and all tests (Testcontainers on the runner, contract
drift failing instead of rewriting), the migration bundle, `pnpm verify`, generated API types against the contract, both
end-to-end suites, and builds of the five images, pushed to GHCR from `main`. Dependabot proposes grouped weekly updates
for NuGet, npm, Docker base images and actions.

## Consequences

- A release is five images and a migration job; nothing else is copied onto servers.
- A CSP change that breaks a feature fails a test, and a feature that needs a new origin fails until the policy says so.
- The policy keeps `style-src 'unsafe-inline'` (React style attributes, the viewer's shadow DOM). Scripts, the real
  injection risk, stay locked down.
- Access tokens of a removed member remain valid for up to 15 minutes, as before (ADR-0006); traces and logs make such
  a request visible.
- **Not done: infrastructure as code.** Without a chosen provider, Terraform for buckets, lifecycle rules, a CDN and a
  managed database would be unverifiable guesses. What it must create is written down instead: the two buckets with the
  CORS rules in `deploy/storage`, public read on the assets bucket only, the `armenu_app` role without `BYPASSRLS`, and
  secrets for the JWT key, the processor token and SMTP.
- Rejected: running migrations at API startup (DDL rights for the runtime, races between instances); an nginx config
  written by hand (drifts from the bundle); `ASPNETCORE_FORWARDEDHEADERS_ENABLED` (trusts every sender); a vendor APM
  agent (OTLP reaches any backend).
