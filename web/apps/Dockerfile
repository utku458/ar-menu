# syntax=docker/dockerfile:1
# A web app as static files behind nginx, with the security headers of web/packages/web-security. Build from web/:
#
#   docker build -f apps/Dockerfile --build-arg APP=guest \
#     --build-arg VITE_API_BASE_URL=https://api.armenu.app --build-arg VITE_ASSETS_ORIGIN=https://cdn.armenu.app \
#     -t armenu-guest .
#
# The VITE_* variables are compiled into the bundle and into its content security policy, so an image belongs to
# one environment. Deployments to a CDN upload the same files and apply the same headers.

FROM node:24-alpine AS build
ARG APP
ENV PNPM_HOME=/pnpm \
    PATH=/pnpm:$PATH
RUN test "$APP" = guest || test "$APP" = dashboard && npm install --global pnpm@12.4.1

WORKDIR /web
COPY pnpm-lock.yaml pnpm-workspace.yaml package.json tsconfig.base.json ./
COPY apps/guest/package.json apps/guest/
COPY apps/dashboard/package.json apps/dashboard/
COPY packages/api-client/package.json packages/api-client/
COPY packages/ar-viewer/package.json packages/ar-viewer/
COPY packages/locale/package.json packages/locale/
COPY packages/web-security/package.json packages/web-security/
RUN pnpm install --frozen-lockfile --filter "@armenu/${APP}..."

COPY packages/api-client packages/api-client
COPY packages/ar-viewer packages/ar-viewer
COPY packages/locale packages/locale
COPY packages/web-security packages/web-security
COPY apps/${APP} apps/${APP}

ARG VITE_API_BASE_URL
ARG VITE_ASSETS_ORIGIN
ARG VITE_GUEST_MENU_BASE_URL
ARG VITE_STORAGE_ORIGINS
ARG VITE_DEMO_MENUS=""
RUN pnpm --filter "@armenu/${APP}" build \
 && node packages/web-security/src/nginx-headers.ts "$APP" > /security-headers.conf \
 && if [ "$APP" = guest ]; then mkdir -p /site/m && cp -R apps/guest/dist/. /site/m/; \
    else mkdir -p /site && cp -R apps/dashboard/dist/. /site/; fi

FROM nginxinc/nginx-unprivileged:1.30-alpine
ARG APP
COPY apps/nginx/${APP}.conf /etc/nginx/conf.d/default.conf
COPY --from=build /security-headers.conf /etc/nginx/security-headers.conf
COPY --from=build /site /usr/share/nginx/html
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s CMD wget -q -O /dev/null http://127.0.0.1:8080/health || exit 1
