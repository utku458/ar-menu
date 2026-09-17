import { describe, expect, it } from 'vitest';

import { fileNameOf } from './menu-transfer.ts';

describe('fileNameOf', () => {
  it('prefers the UTF-8 name and falls back to the plain one', () => {
    expect(fileNameOf(`attachment; filename=kofteci-menu.csv; filename*=UTF-8''k%C3%B6fteci-menu.csv`)).toBe(
      'köfteci-menu.csv',
    );
    expect(fileNameOf('attachment; filename="menu.csv"')).toBe('menu.csv');
    expect(fileNameOf(null)).toBeUndefined();
  });
});
