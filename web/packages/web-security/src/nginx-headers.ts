#!/usr/bin/env node
/**
 * Prints the security headers of a web app as nginx `add_header` directives, for the container image:
 *
 *   node nginx-headers.ts guest > security-headers.conf
 *
 * Reads the same `VITE_*` variables the build was given (environment variables, as in the Dockerfile).
 */
import { securityHeaders } from './headers.ts';

const app = process.argv[2];
if (app !== 'guest' && app !== 'dashboard') {
  process.stderr.write('Usage: nginx-headers.ts <guest|dashboard>\n');
  process.exit(2);
}

const lines = Object.entries(securityHeaders(app, process.env)).map(
  // "always": error pages carry the headers too. Values contain no double quotes, the only character that would end the nginx string.
  ([name, value]) => `add_header ${name} "${value}" always;`,
);
process.stdout.write(`${lines.join('\n')}\n`);
