import { describe, expect, test } from 'vitest';

import { arRate, niceMax } from './statistics-api.ts';

describe('statistics helpers', () => {
  test('the axis ends at a clean number', () => {
    expect([0, 1, 3, 7, 12, 51, 99, 101, 480].map(niceMax)).toEqual([1, 1, 3, 8, 15, 60, 100, 150, 500]);
  });

  test('the AR rate is a share of openings, undefined without openings', () => {
    expect(arRate(0, 0)).toBeUndefined();
    expect(arRate(8, 3)).toBe(38);
    // Counted once per page load each, AR starts can outnumber openings of the same load only through links.
    expect(arRate(2, 5)).toBe(100);
  });
});
