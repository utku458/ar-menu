import { readFile } from 'node:fs/promises';

import { expect, test } from 'vitest';

import { renderSchema, schemaPath } from '../scripts/schema.ts';

// The API side verifies contracts/openapi/v1.json against the running code (OpenApiContractTests);
// this closes the loop by verifying the committed TypeScript types against that document.
test('generated API types are in sync with the OpenAPI contract', async () => {
  const [committed, fresh] = await Promise.all([readFile(schemaPath, 'utf8'), renderSchema()]);

  expect(committed, 'API types are stale: run `pnpm generate:api` and commit the result.').toBe(fresh);
});
