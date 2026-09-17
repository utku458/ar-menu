export type LogLevel = 'info' | 'error';

export type Logger = Readonly<Record<LogLevel, (message: string, fields?: Record<string, unknown>) => void>>;

/** One JSON object per line on stdout, which container platforms collect and index as is. */
export const jsonLogger: Logger = {
  info: (message, fields) => {
    write('info', message, fields);
  },
  error: (message, fields) => {
    write('error', message, fields);
  },
};

export const silentLogger: Logger = { info: () => undefined, error: () => undefined };

function write(level: LogLevel, message: string, fields: Record<string, unknown> = {}): void {
  process.stdout.write(`${JSON.stringify({ time: new Date().toISOString(), level, message, ...fields })}\n`);
}
