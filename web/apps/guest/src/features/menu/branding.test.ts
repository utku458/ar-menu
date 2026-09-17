import { describe, expect, test } from 'vitest';

import { applyBranding, clearBranding, type BrandTarget } from './branding.ts';

/** Stands in for the document element: the two style methods the menu uses, backed by a map. */
function target() {
  const properties = new Map<string, string>();
  return {
    style: {
      setProperty: (name: string, value: string) => properties.set(name, value),
      removeProperty: (name: string) => {
        properties.delete(name);
        return '';
      },
    },
    get: (name: string) => properties.get(name),
  } satisfies BrandTarget & { get: (name: string) => string | undefined };
}

describe('branding', () => {
  test('paints the menu in the colour the API sent, with the text it paired with it', () => {
    const root = target();

    applyBranding(root, { accentColor: '#1f6f5c', onAccentColor: '#ffffff' });

    expect(root.get('--color-brand')).toBe('#1f6f5c');
    expect(root.get('--color-brand-ink')).toBe('#ffffff');
  });

  test('leaves the platform’s own colours in place for a business without one', () => {
    const root = target();

    applyBranding(root, { accentColor: null, onAccentColor: null });

    expect(root.get('--color-brand')).toBeUndefined();
  });

  test('switching to a business without a colour takes the previous one away', () => {
    const root = target();
    applyBranding(root, { accentColor: '#1f6f5c', onAccentColor: '#ffffff' });

    applyBranding(root, { accentColor: null, onAccentColor: null });

    expect(root.get('--color-brand')).toBeUndefined();
    expect(root.get('--color-brand-ink')).toBeUndefined();
  });

  test('leaving the menu puts the platform’s colours back', () => {
    const root = target();
    applyBranding(root, { accentColor: '#1f6f5c', onAccentColor: '#ffffff' });

    clearBranding(root);

    expect(root.get('--color-brand')).toBeUndefined();
  });
});
