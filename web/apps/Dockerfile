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
# A template rather than a finished config: the image's entrypoint renders it at start-up, which is what lets one
# dashboard image be pointed at an API origin it could not know at build time (see apps/nginx/dashboard.conf).
#
# The filter is the safety catch. Without it the entrypoint would substitute every environment variable it finds,
# and an nginx config is full of $host, $uri and $request_uri, which are nginx's variables and not the shell's.
#
# NGINX_ENTRYPOINT_LOCAL_RESOLVERS makes the image publish the container's own nameservers as
# NGINX_LOCAL_RESOLVERS before the templates are rendered, bracketing IPv6 addresses the way nginx wants them —
# which is what a private network the API may move around on needs. The literal below is only a fallback.
COPY apps/nginx/${APP}.conf /etc/nginx/templates/default.conf.template
ENV API_ORIGIN="" \
    NGINX_ENTRYPOINT_LOCAL_RESOLVERS=1 \
    NGINX_LOCAL_RESOLVERS=127.0.0.11 \
    NGINX_ENVSUBST_FILTER="^(API_ORIGIN|NGINX_LOCAL_RESOLVERS)$"
COPY --from=build /security-headers.conf /etc/nginx/security-headers.conf
COPY --from=build /site /usr/share/nginx/html
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s CMD wget -q -O /dev/null http://127.0.0.1:8080/health || exit 1
