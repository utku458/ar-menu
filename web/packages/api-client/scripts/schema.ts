import { fileURLToPath } from 'node:url';

import openapiTS, { astToString } from 'openapi-typescript';

/** The OpenAPI document published by the API and verified by its OpenApiContractTests. */
const contractUrl = new URL('../../../../contracts/openapi/v1.json', import.meta.url);

export const schemaPath = fileURLToPath(new URL('../src/schema.gen.ts', import.meta.url));

const header = '// Generated from contracts/openapi/v1.json by `pnpm generate:api`. Do not edit.\n\n';

export async function renderSchema(): Promise<string> {
  const ast = await openapiTS(contractUrl, {
    // Named aliases (`PublicMenuResponse`) next to `components['schemas'][...]`.
    rootTypes: true,
    rootTypesNoSchemaPrefix: true,
  });

  return header + astToString(ast);
}
