import { writeFile } from 'node:fs/promises';

import { renderSchema, schemaPath } from './schema.ts';

await writeFile(schemaPath, await renderSchema());
console.log(`API types written to ${schemaPath}`);
