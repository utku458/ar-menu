import { describe, expect, test } from 'vitest';

import { safeRedirect, slugify } from './slug.ts';

describe('slugify', () => {
  test.each([
    ['Kadıköy Burger Lab', 'kadikoy-burger-lab'],
    ['ÇİĞ KÖFTE & Dürüm', 'cig-kofte-durum'],
    ['  Café Müller  ', 'cafe-muller'],
    ['Şişli Balıkçısı', 'sisli-balikcisi'],
  ])('%s → %s', (name, slug) => {
    expect(slugify(name)).toBe(slug);
  });
});

describe('safeRedirect', () => {
  test.each([
    ['/cafe/menu?category=1', '/cafe/menu?category=1'],
    ['//evil.example/phish', undefined],
    ['https://evil.example', undefined],
    [undefined, undefined],
  ])('%s → %s', (target, expected) => {
    expect(safeRedirect(target)).toBe(expected);
  });
});
