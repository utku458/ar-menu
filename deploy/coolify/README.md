# Running ArMenu on Coolify (Hetzner)

The whole platform runs in one Coolify project, `armenu`, on the Hetzner server `coolify-1`. Every application is
built on the server from this repository through Coolify's GitHub App.

| Application       | Build pack / Dockerfile                    | Base directory | Port | Public? | Health          |
| ----------------- | ------------------------------------------ | -------------- | ---- | ------- | --------------- |
| `Postgres`        | Coolify PostgreSQL 18                      | —              | 5432 | no      | —               |
| `storage`         | `/deploy/coolify/storage.Dockerfile`       | `/`            | 8333 | yes     | `/healthz`      |
| `api`             | `/src/ArMenu.Api/Dockerfile`               | `/`            | 8080 | yes     | `/health/ready` |
| `asset-processor` | `/services/asset-processor/Dockerfile`     | `/web`         | 5090 | no      | —               |
| `guest`           | `/apps/Dockerfile` (`APP=guest`)           | `/web`         | 8080 | yes     | `/health`       |
| `dashboard`       | `/apps/Dockerfile` (`APP=dashboard`)       | `/web`         | 8080 | yes     | `/health`       |

Two base directories, because a Docker build's context is the base directory and the web images expect to be built
from `web/` while the API image expects the repository root. `storage` needs **Persistent Storage** mounted at
`/data` — uploads and published assets live there, and without it every redeploy starts empty.

Coolify's in-container health check is off: the images ship without `curl`/`wget`. The paths above are checked from
outside instead.

## Deploying: CI is the only path to production

**Auto Deploy is off** on all five applications. A push to `main` runs `.github/workflows/ci.yml`; only when
`dotnet`, `web`, `alerts` and `images` have all passed does the `deploy` job call Coolify's API for the five
applications at once and wait for every build to finish. A failing test, or a build that fails on the server, turns
the run red and nothing half-ships.

The job needs two repository secrets (*Settings → Secrets and variables → Actions*):

| Secret               | Value                                                                                       |
| -------------------- | ------------------------------------------------------------------------------------------- |
| `COOLIFY_DEPLOY_URL` | `https://coolify.46.225.37.55.sslip.io/api/v1/deploy?uuid=<api>,<asset-processor>,<dashboard>,<guest>,<storage>` |
| `COOLIFY_TOKEN`      | a Coolify API token with the `deploy` and `read` permissions                                |

Deploy by hand with `gh workflow run ci.yml`, or **Redeploy** in the Coolify panel.

**Migrations are not part of the deploy.** If a change adds a migration, run `deploy/coolify/migrate.sh` (below)
*before* merging it, so the new API never starts against an old schema.

## Two things to know

**The web apps are compiled for one environment.** `VITE_API_BASE_URL` and friends are baked into the JavaScript
*and* into the content security policy that nginx serves (`packages/web-security`). Coolify passes them as build
arguments (mark them **Build Variable**), and `apps/Dockerfile` declares them as `ARG` — so changing a domain means
redeploying the app that points at it, not just restarting it.

**The dashboard talks to the API through its own origin.** The refresh token is an HttpOnly, `SameSite=Strict`
cookie, and generated hosting domains such as `*.sslip.io` make the API and the dashboard *different sites* to a
browser: a cross-origin dashboard would sign in and then never be able to refresh. The dashboard's nginx therefore
proxies `/api/` to the API (see `apps/nginx/dashboard.conf`), which makes the cookie first-party and takes CORS out
of the picture. The guest menu has no session and calls the API cross-origin as usual.

## Variables

Generate the four secrets once and keep them; several applications need the same value:

```bash
openssl rand -base64 32   # Authentication__Jwt__SigningKey
openssl rand -hex 24      # ARMENU_APP_PASSWORD  (the database password the API uses)
openssl rand -hex 24      # STORAGE_SECRET_KEY
openssl rand -hex 24      # AssetProcessor__Token (must be at least 32 characters)
```

Applications in one Coolify project share a Docker network, so they reach each other by container name — take the
names (`<postgres-host>`, `<api-host>`, `<asset-processor-host>`) from each resource's internal URL in the panel.

### storage

```
STORAGE_ACCESS_KEY=armenu-api
STORAGE_SECRET_KEY=<the storage secret you generated>
```

### api

```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__ArMenu=Host=<postgres-host>;Port=5432;Database=railway;Username=armenu_app;Password=<ARMENU_APP_PASSWORD>;SSL Mode=Prefer;Trust Server Certificate=true
Authentication__Jwt__SigningKey=<the base64 signing key you generated>

Assets__PublicBaseUrl=https://<storage-domain>/armenu-assets/
Storage__ServiceUrl=https://<storage-domain>
Storage__PublicServiceUrl=https://<storage-domain>
Storage__AccessKey=armenu-api
Storage__SecretKey=<the storage secret>
Storage__ForcePathStyle=true

AssetProcessor__Url=http://<asset-processor-host>:5090/
AssetProcessor__Token=<the processor token>

Dashboard__Url=https://<dashboard-domain>/
Cors__AllowedOrigins__0=https://<guest-domain>

ReverseProxy__KnownNetworks__0=<the coolify Docker network, e.g. from `docker network inspect coolify`>
ReverseProxy__ForwardLimit=1
```

The database is named `railway` for historical reasons: the data kept its name when it moved. Renaming it buys
nothing.

`Storage__ServiceUrl` is the public URL on purpose. The API signs upload URLs for browsers and download URLs for the
asset processor; if it signed them for a host only reachable inside the server, neither could use them.

`ReverseProxy__KnownNetworks` names the network Coolify's proxy (Traefik) sends from: rate limits and client
addresses come from `X-Forwarded-For`, which the API ignores from senders it does not trust. With `ForwardLimit=1`
the API takes the *last* entry, the one Traefik appended, so a client cannot pick its own address.

### asset-processor

```
ASSET_PROCESSOR_PORT=5090
ASSET_PROCESSOR_TOKEN=<the processor token, same as the API's>
ASSET_PROCESSOR_STORAGE_ORIGINS=https://<storage-domain>
```

No domain: the API reaches it over the Docker network. It renders in a headless Chrome, so give it more memory than
the others if models fail to process.

### guest

```
APP=guest
VITE_API_BASE_URL=https://<api-domain>
VITE_ASSETS_ORIGIN=https://<storage-domain>
```

### dashboard

```
APP=dashboard
API_ORIGIN=http://<api-host>:8080
VITE_API_BASE_URL=https://<dashboard-domain>
VITE_GUEST_MENU_BASE_URL=https://<guest-domain>/m/
VITE_STORAGE_ORIGINS=https://<storage-domain>
```

`VITE_API_BASE_URL` is the dashboard's **own** domain: requests go to `/api/…` on this origin and nginx forwards
them to `API_ORIGIN` over the Docker network.

## The two one-off steps

Both are infrastructure, deliberately not done by the application at startup. Run them from a machine with Docker,
from the repository root.

### Database role and schema

The database is not reachable from outside the server, so open an SSH tunnel to the Postgres container first:

```bash
ssh -N -L 15432:<postgres-container-ip>:5432 root@46.225.37.55 &
PGHOST=host.docker.internal PGPORT=15432 PGUSER=postgres PGPASSWORD='<postgres password>' PGDATABASE=railway \
ARMENU_APP_PASSWORD='<the same value as the API>' \
deploy/coolify/migrate.sh
```

This creates (or re-passwords) the unprivileged `armenu_app` role the API connects as — no superuser, owner of
nothing, no row-level-security bypass — and then applies the migration bundle as the schema owner. Re-running is
safe.

### Storage buckets and CORS

```bash
STORAGE_URL=https://<storage-domain> \
DASHBOARD_URL=https://<dashboard-domain> \
AWS_ACCESS_KEY_ID=armenu-api AWS_SECRET_ACCESS_KEY='<the storage secret>' \
deploy/coolify/provision-storage.sh
```

Creates `armenu-uploads` (private; the dashboard's origin is the only one allowed to `PUT`) and `armenu-assets`
(readable by anyone, which is what lets a guest's browser fetch posters and models). Re-run it whenever the
dashboard's domain changes.

## Check

```bash
curl -sS https://<api-domain>/health/ready
curl -sS -o /dev/null -w '%{http_code}\n' https://<guest-domain>/m/
curl -sSI https://<dashboard-domain>/ | grep -i content-security-policy
```

Then sign in on the dashboard, wait a few minutes and reload: if you are still signed in, the same-origin proxy is
doing its job.

## Backups

Coolify's **Scheduled Backups** on `Postgres` run daily and are also sent to S3. The `storage` volume is not covered
by them: back up `/data` separately if uploads must survive the loss of the server.

## The platform administrator

The API creates its administrator on first start, with the user name and password of
`PlatformAdministrator:UserName` and `PlatformAdministrator:InitialPassword` — `admin` and `admin1234` unless the
service says otherwise. Set them per deployment with `PlatformAdministrator__UserName` and
`PlatformAdministrator__InitialPassword`; both are read only while no administrator exists, and never again.

**On a public address `admin1234` is a known credential.** Change it at the first sign-in (the platform page has the
form), or set a different one before the first start. The account is not otherwise privileged in a way a password
protects against: it opens businesses and runs every one of them.

It signs in like anyone else, at the dashboard's one sign-in page, and lands on the platform page rather than in a
business. From there it opens businesses — each with its owner's user name and password, which the administrator
passes on — and steps into any of them to run it as the owner would.

Two things follow from the administrator being a member of no business, and are deliberate:

- Entering a business gives a short-lived access token, not a session. It cannot be refreshed, so the dashboard
  enters again as needed, and the administrator's access to a business ends when its own session does.
- The business's team never lists the administrator, and the history records what it changed under its own name.

Self-service sign-up is closed unless `Onboarding__SignUpEnabled` is `true`: businesses are opened by the
administrator. Password resets are done by the owner (for accounts with a user name) or by the administrator, since
an account opened this way has no mailbox to send a link to.

## Notes

**`Email__Host` is required, even with no mail server.** The API validates its options at startup and refuses to
start without it — `Email:Host is required.` is a crash, not a warning. Sending mail, on the other hand, really is
optional: messages go to a durable outbox, so sign-up, sign-in and menu editing work whether or not delivery
succeeds. Only team invitations and password resets need a message to arrive.

For a deployment with no mail server, `Email__Host=localhost` satisfies the check and every send fails fast against
a closed port; the outbox retries a bounded number of times and gives up. For real delivery, point `Email__Host`,
`Email__Port`, `Email__Security`, `Email__UserName` and `Email__Password` at an SMTP provider. A user can be marked
verified by hand with `UPDATE "Users" SET "EmailVerifiedAt" = now() WHERE "Email" = '…';`.

**Production data has no demo menus.** The seeded demo restaurants exist only in the Development environment, so the
first menu is one you create in the dashboard.
