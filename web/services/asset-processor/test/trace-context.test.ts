import { describe, expect, it } from 'vitest';

import { traceContextOf } from '../src/trace-context.ts';

describe('traceContextOf', () => {
  it('reads the trace and parent span of a W3C traceparent header', () => {
    expect(traceContextOf('00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01')).toEqual({
      traceId: '4bf92f3577b34da6a3ce929d0e0e4736',
      parentSpanId: '00f067aa0ba902b7',
    });
  });

  it.each([
    undefined,
    '',
    'not-a-traceparent',
    '01-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01',
    '00-00000000000000000000000000000000-00f067aa0ba902b7-01',
    '00-4bf92f3577b34da6a3ce929d0e0e4736-0000000000000000-01',
    '00-4BF92F3577B34DA6A3CE929D0E0E4736-00f067aa0ba902b7-01',
  ])('ignores %j', (header) => {
    expect(traceContextOf(header)).toBeUndefined();
  });
});
