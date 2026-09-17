export interface TraceContext {
  readonly traceId: string;
  readonly parentSpanId: string;
}

const traceparent = /^00-(?<traceId>[0-9a-f]{32})-(?<parentSpanId>[0-9a-f]{16})-[0-9a-f]{2}$/;
const invalidTraceId = '0'.repeat(32);
const invalidSpanId = '0'.repeat(16);

/**
 * The W3C trace context the API sends with every job (its HTTP client instrumentation adds `traceparent`). Logged with
 * the job, it joins the processor's lines to the API's trace of the same processing.
 */
export function traceContextOf(header: string | string[] | undefined): TraceContext | undefined {
  const match = typeof header === 'string' ? traceparent.exec(header.trim()) : null;
  const { traceId, parentSpanId } = match?.groups ?? {};
  return traceId === undefined ||
    parentSpanId === undefined ||
    traceId === invalidTraceId ||
    parentSpanId === invalidSpanId
    ? undefined
    : { traceId, parentSpanId };
}
