import { ModelRenderer, processModel } from '@armenu/model-pipeline';

import { readConfig } from './config.ts';
import { jsonLogger as logger } from './logger.ts';
import { createProcessorServer } from './server.ts';

const config = readConfig(process.env);

let renderer = await ModelRenderer.launch({ channel: config.browserChannel });

/** A crashed browser is replaced before the next job instead of failing every job after it. */
async function liveRenderer(): Promise<ModelRenderer> {
  if (!renderer.isConnected) {
    logger.error('The browser disconnected; relaunching it');
    renderer = await ModelRenderer.launch({ channel: config.browserChannel });
  }
  return renderer;
}

const { server, close } = createProcessorServer(config, {
  logger,
  process: async (source) => processModel(source, await liveRenderer()),
});

server.listen(config.port, () => {
  logger.info('Asset processor listening', { port: config.port, concurrency: config.concurrency });
});

for (const signal of ['SIGTERM', 'SIGINT'] as const) {
  process.once(signal, () => {
    logger.info('Shutting down; finishing running jobs', { signal });
    void close()
      .then(() => renderer.close())
      .finally(() => process.exit(0));
  });
}
