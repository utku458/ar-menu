# syntax=docker/dockerfile:1
# Build from web/: docker build -f services/asset-processor/Dockerfile -t armenu-asset-processor web
#
# Playwright's image ships Node.js and the Chromium build playwright-core expects, with the libraries WebGL needs
# (SwiftShader renders without a GPU). The service runs its TypeScript sources directly with Node's type stripping.
FROM mcr.microsoft.com/playwright:v1.63.0-noble

ENV NODE_ENV=production \
    PNPM_HOME=/pnpm \
    PATH=/pnpm:$PATH
RUN npm install --global pnpm@12.4.1

WORKDIR /app

# Dependencies first, so source changes reuse the installed layer.
COPY pnpm-lock.yaml pnpm-workspace.yaml package.json ./
COPY packages/ar-viewer/package.json packages/ar-viewer/
COPY packages/model-pipeline/package.json packages/model-pipeline/
COPY services/asset-processor/package.json services/asset-processor/
RUN pnpm install --frozen-lockfile --prod --filter "@armenu/asset-processor..."

COPY packages/ar-viewer/src packages/ar-viewer/src
COPY packages/model-pipeline/src packages/model-pipeline/src
COPY services/asset-processor/src services/asset-processor/src

USER pwuser
EXPOSE 5090
HEALTHCHECK --interval=30s --timeout=3s CMD node -e "fetch('http://127.0.0.1:' + (process.env.ASSET_PROCESSOR_PORT ?? 5090) + '/health/live').then((r) => process.exit(r.ok ? 0 : 1), () => process.exit(1))"
CMD ["node", "services/asset-processor/src/main.ts"]
