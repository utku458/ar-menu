# Running ArMenu on Railway

The whole platform on Railway: one project, six services, built from this repository by Railway's GitHub integration.

| Service           | Source                                 | Root directory | Public? |
| ----------------- | -------------------------------------- | -------------- | ------- |
| `postgres`        | Railway's PostgreSQL                   | —              | no      |
| `storage`         | `deploy/railway/storage.Dockerfile`    | `/`            | yes     |
| `api`             | `src/ArMenu.Api/Dockerfile`            | `/`            | yes     |
| `asset-processor` | `services/asset-processor/Dockerfile`  | `/web`         | no      |
| `guest`           | `apps/Dockerfile` (`APP=guest`)        | `/web`         | yes     |
| `dashboard`       | `apps/Dockerfile` (`APP=dashboard`)    | `/web`         | yes     |

Two root directories, because a Docker build's context is the service's root directory and the web images expect to
be built from `web/` while the API image expects the repository root.

## Two things to know before you start

**The web apps are compiled for one environment.** `VITE_API_BASE_URL` and friends are baked into the JavaScript
*and* into the content security policy that nginx serves (`packages/web-security`). Railway passes service variables
to the Docker build as build arguments, and `apps/Dockerfile` declares them as `ARG`, so this works — but changing a
domain means redeploying the app that points at it, not just restarting it. That is why the domains are generated
first, below, and the variables set afterwards.

**The dashboard talks to the API through its own origin.** The refresh token is an HttpOnly, `SameSite=Strict`
cookie, and `up.railway.app` is on the Public Suffix List, so `…-api.up.railway.app` and `…-dashboard.up.railway.app`
are *different sites* to a browser: a cross-origin dashboard would sign in and then never be able to refresh. The
dashboard's nginx therefore proxies `/api/` to the API (see `apps/nginx/dashboard.conf`), which makes the cookie
first-party and takes CORS out of the picture. The guest menu is unaffected — it has no session — and calls the API
cross-origin as usual.

If you later put everything under one domain of your own (`app.example.com`, `api.example.com`), you can drop
`API_ORIGIN` and point `VITE_API_BASE_URL` straight at the API instead. Nothing else changes.

## 1. Project, database and secrets

Create a Railway project from this GitHub repository, then add a **PostgreSQL** database to it.

Generate the four secrets once and keep them; several services need the same value:

```bash
openssl rand -base64 32   # Authentication__Jwt__SigningKey
openssl rand -hex 24      # ARMENU_APP_PASSWORD  (the database password the API uses)
openssl rand -hex 24      # STORAGE_SECRET_KEY
openssl rand -hex 24      # AssetProcessor__Token (must be at least 32 characters)
```

`STORAGE_ACCESS_KEY` is not a secret; `armenu-api` is a fine value.

## 2. Create the five deployed services

Add each one from the same repository. For every service, set **Settings → Source → Root Directory** and add a
`RAILWAY_DOCKERFILE_PATH` variable from the table at the top. Then, for the four public ones, **Settings →
Networking → Generate Domain**.

`api`, `guest` and `dashboard` all listen on 8080 and `storage` on 8333; set `PORT` accordingly on each (it is in the
variable lists below) so Railway routes to the right port.

`storage` also needs a **Volume mounted at `/data`** — that is where uploads and published assets live, and without
it every redeploy starts empty.

Deployments will fail until the variables below are set. That is expected.

## 3. Variables

Replace `<…-domain>` with the domains Railway generated. `${{…}}` is Railway's own reference syntax — type it
literally, the dashboard will resolve it.

### storage

```
PORT=8333
STORAGE_ACCESS_KEY=armenu-api
STORAGE_SECRET_KEY=<the storage secret you generated>
RAILWAY_DOCKERFILE_PATH=deploy/railway/storage.Dockerfile
```

### api

```
PORT=8080
RAILWAY_DOCKERFILE_PATH=src/ArMenu.Api/Dockerfile
ASPNETCORE_ENVIRONMENT=Production

ARMENU_APP_PASSWORD=<the database password you generated>
ConnectionStrings__ArMenu=Host=${{Postgres.RAILWAY_PRIVATE_DOMAIN}};Port=5432;Database=${{Postgres.PGDATABASE}};Username=armenu_app;Password=${{ARMENU_APP_PASSWORD}};SSL Mode=Prefer;Trust Server Certificate=true

Authentication__Jwt__SigningKey=<the base64 signing key you generated>

Assets__PublicBaseUrl=https://<storage-domain>/armenu-assets/
Storage__ServiceUrl=https://<storage-domain>
Storage__PublicServiceUrl=https://<storage-domain>
Storage__AccessKey=armenu-api
Storage__SecretKey=<the storage secret>
Storage__ForcePathStyle=true

AssetProcessor__Url=http://${{asset-processor.RAILWAY_PRIVATE_DOMAIN}}:5090/
AssetProcessor__Token=<the processor token>

Dashboard__Url=https://<dashboard-domain>/
Cors__AllowedOrigins__0=https://<guest-domain>

ReverseProxy__KnownNetworks__0=0.0.0.0/0
ReverseProxy__KnownNetworks__1=::/0
ReverseProxy__ForwardLimit=1
```

Only the guest menu is listed under CORS: the dashboard reaches the API through its own origin, so its requests are
never cross-origin.

`Storage__ServiceUrl` is the public URL rather than the private one on purpose. The API signs upload URLs for
browsers and download URLs for the asset processor, and if it signed them for a host only reachable inside the
project, neither could use them. Everything then agrees on one hostname.

`ReverseProxy__KnownNetworks` exists because rate limits and client addresses come from `X-Forwarded-For`, which the
API ignores from senders it does not trust — and Railway's edge has no fixed address to name. With `ForwardLimit=1`
the API takes the *last* entry, which is the one Railway's edge appended itself, so a client that sends its own
`X-Forwarded-For` header cannot pick its address. Requests that arrive through the dashboard's nginx are attributed
to that nginx rather than to the visitor, which shares the dashboard's authentication rate limit across its users.

### asset-processor

```
RAILWAY_DOCKERFILE_PATH=services/asset-processor/Dockerfile
ASSET_PROCESSOR_PORT=5090
ASSET_PROCESSOR_TOKEN=<the processor token, same as the API's>
ASSET_PROCESSOR_STORAGE_ORIGINS=https://<storage-domain>
```

No public domain: the API reaches it over the private network. It renders in a headless Chrome, so give it more
memory than the others if models fail to process.

### guest

```
PORT=8080
RAILWAY_DOCKERFILE_PATH=apps/Dockerfile
APP=guest
VITE_API_BASE_URL=https://<api-domain>
VITE_ASSETS_ORIGIN=https://<storage-domain>
```

### dashboard

```
PORT=8080
RAILWAY_DOCKERFILE_PATH=apps/Dockerfile
APP=dashboard
API_ORIGIN=http://${{api.RAILWAY_PRIVATE_DOMAIN}}:8080
VITE_API_BASE_URL=https://<dashboard-domain>
VITE_GUEST_MENU_BASE_URL=https://<guest-domain>/m/
VITE_STORAGE_ORIGINS=https://<storage-domain>
```

`VITE_API_BASE_URL` is the dashboard's **own** domain: requests go to `/api/…` on this origin and nginx forwards
them. `API_ORIGIN` is where it forwards them to, over the private network. If the private address does not resolve,
`https://<api-domain>` works too.

Set **Settings → Deploy → Health Check Path** to `/health` for `guest` and `dashboard`, and `/health/ready` for
`api`.

## 4. The two one-off steps

Both are infrastructure, deliberately not done by the application at startup. Run them from a machine with Docker,
from the repository root.

### Database role and schema

Take the **public** connection values from the Postgres service's *Connect* tab (`postgres.railway.internal` is only
reachable from inside the project):

```bash
PGHOST=<proxy-host> PGPORT=<proxy-port> PGUSER=postgres PGPASSWORD='<postgres password>' PGDATABASE=railway \
ARMENU_APP_PASSWORD='<the same value as the API service>' \
deploy/railway/migrate.sh
```

This creates the unprivileged `armenu_app` role the API connects as — no superuser, owner of nothing, and no
row-level-security bypass — and then applies the migration bundle as the schema owner.

### Storage buckets and CORS

```bash
STORAGE_URL=https://<storage-domain> \
DASHBOARD_URL=https://<dashboard-domain> \
AWS_ACCESS_KEY_ID=armenu-api AWS_SECRET_ACCESS_KEY='<the storage secret>' \
deploy/railway/provision-storage.sh
```

Creates `armenu-uploads` (private, and the dashboard's origin is the only one allowed to `PUT`) and `armenu-assets`
(readable by anyone, which is what lets a guest's browser fetch posters and models). Re-run it whenever the
dashboard's domain changes.

## 5. Redeploy and check

Redeploy `api`, `guest` and `dashboard` so the variables and build arguments take effect, then:

```bash
curl -sS https://<api-domain>/health/ready
curl -sS -o /dev/null -w '%{http_code}\n' https://<guest-domain>/m/
curl -sSI https://<dashboard-domain>/ | grep -i content-security-policy
```

Then open the dashboard, create an account, add a business and a menu, and open the guest menu at
`https://<guest-domain>/m/<slug>`. Sign in, wait a few minutes and reload the dashboard: if you are still signed in,
the same-origin proxy is doing its job.

## Notes

**E-mail is optional.** Without `Email__*` configured, sign-up, sign-in and menu editing all work; only team
invitations and password resets, which need to send a message, do not. Add `Email__Host`, `Email__Port`,
`Email__Security`, `Email__Username` and `Email__Password` for any SMTP provider when you want them. A user can be
marked verified by hand with `UPDATE "Users" SET "EmailVerifiedAt" = now() WHERE "Email" = '…';`.

**Production data has no demo menus.** The seeded demo restaurants exist only in the Development environment, so the
first menu is one you create in the dashboard.

**Cost.** Six services plus a volume is more than the Hobby plan's monthly credit comfortably covers, and the asset
processor is the expensive one: it carries a full Chromium and only does anything while a model is being processed.
If the bill matters more than instant 3D processing, that is the service to scale to zero between uploads.
