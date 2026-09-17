import { describe, expect, test } from 'vitest';

import { ApiError } from '../src/api-error.ts';

describe('ApiError', () => {
  test('keeps the problem details and exposes their stable code', () => {
    const response = new Response(null, { status: 404 });

    const error = ApiError.fromResponse(response, {
      status: 404,
      detail: 'No active tenant matches this request.',
      code: 'tenant.not_found',
    });

    expect(error.status).toBe(404);
    expect(error.code).toBe('tenant.not_found');
    expect(error.message).toBe('No active tenant matches this request.');
  });

  test.each([
    ['30', 30],
    [' 5 ', 5],
    ['Wed, 21 Oct 2026 07:28:00 GMT', undefined],
    ['-1', undefined],
  ])('reads Retry-After %j as %j seconds', (header, expected) => {
    const response = new Response(null, { status: 429, headers: { 'Retry-After': header } });

    expect(ApiError.fromResponse(response, undefined).retryAfterSeconds).toBe(expected);
  });

  test('describes responses without a problem body by their status', () => {
    const error = ApiError.fromResponse(
      new Response('<html>Bad gateway</html>', { status: 502 }),
      'Bad gateway',
    );

    expect(error.problem).toBeUndefined();
    expect(error.message).toBe('The API answered with status 502.');
  });
});
