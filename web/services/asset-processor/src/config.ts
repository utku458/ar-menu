export interface ProcessorConfig {
  readonly port: number;
  /** Shared secret the API sends as a bearer token. */
  readonly token: string;
  /**
   * Origins the presigned URLs may point at. Even with a leaked token, the processor cannot be used to reach anything
   * but object storage.
   */
  readonly storageOrigins: ReadonlySet<string>;
  /** Jobs processed at once. Each one decodes textures and renders in Chrome, so this is bounded by memory. */
  readonly concurrency: number;
  readonly maxSourceBytes: number;
  readonly browserChannel: string | undefined;
}

const minimumTokenLength = 32;

export function readConfig(environment: NodeJS.ProcessEnv): ProcessorConfig {
  const problems: string[] = [];
  const text = (name: string) => environment[name]?.trim() ?? '';
  const integer = (name: string, fallback: number) => {
    const value = text(name) === '' ? fallback : Number(text(name));
    if (!Number.isInteger(value) || value <= 0) {
      problems.push(`${name} must be a positive integer.`);
    }
    return value;
  };

  const token = text('ASSET_PROCESSOR_TOKEN');
  if (token.length < minimumTokenLength) {
    problems.push(`ASSET_PROCESSOR_TOKEN must be at least ${minimumTokenLength} characters.`);
  }

  const storageOrigins = new Set<string>();
  for (const origin of text('ASSET_PROCESSOR_STORAGE_ORIGINS')
    .split(',')
    .filter((entry) => entry.trim() !== '')) {
    try {
      storageOrigins.add(new URL(origin.trim()).origin);
    } catch {
      problems.push(`'${origin}' in ASSET_PROCESSOR_STORAGE_ORIGINS is not a URL.`);
    }
  }
  if (storageOrigins.size === 0) {
    problems.push('ASSET_PROCESSOR_STORAGE_ORIGINS must list the object storage origin(s).');
  }

  const config: ProcessorConfig = {
    port: integer('ASSET_PROCESSOR_PORT', 5090),
    token,
    storageOrigins,
    concurrency: integer('ASSET_PROCESSOR_CONCURRENCY', 1),
    maxSourceBytes: integer('ASSET_PROCESSOR_MAX_SOURCE_BYTES', 64 * 1024 * 1024),
    browserChannel: text('ASSET_PROCESSOR_BROWSER_CHANNEL') || undefined,
  };

  if (problems.length > 0) {
    throw new Error(`Invalid asset processor configuration:\n- ${problems.join('\n- ')}`);
  }

  return config;
}
