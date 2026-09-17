import { describe, expect, test } from 'vitest';

import { formatBytes, formatDimensions, isActive, reductionPercent } from './processing.ts';

describe('processing reports', () => {
  test('sizes read like a file manager', () => {
    expect(formatBytes(18_350_211, 'en-US')).toBe('17.5 MB');
    expect(formatBytes(38_112, 'en-US')).toBe('37.2 kB');
    expect(formatBytes(512, 'en-US')).toBe('512 byte');
    expect(formatBytes(1_342_877, 'tr-TR')).toBe('1,3 MB');
  });

  test('the reduction is a whole percentage and never negative', () => {
    expect(reductionPercent(18_350_211, 1_342_877)).toBe(93);
    expect(reductionPercent(100, 120)).toBe(0);
    expect(reductionPercent(0, 10)).toBe(0);
  });

  test('dimensions are width × depth × height in centimeters, the way a plate is described', () => {
    expect(formatDimensions({ width: 0.182, height: 0.114, depth: 0.179 }, 'en-US')).toBe(
      '18.2 × 17.9 × 11.4 cm',
    );
    expect(formatDimensions({ width: 0.25, height: 0.05, depth: 0.25 }, 'tr-TR')).toBe('25 × 25 × 5 cm');
  });

  test('only queued and running processings are polled', () => {
    expect(isActive('queued')).toBe(true);
    expect(isActive('processing')).toBe(true);
    expect(isActive('succeeded')).toBe(false);
    expect(isActive(undefined)).toBe(false);
  });
});
