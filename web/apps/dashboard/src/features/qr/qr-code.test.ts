import { describe, expect, test } from 'vitest';

import { menuUrl, qrCode, qrSvgFile } from './qr-code.ts';

describe('menuUrl', () => {
  test('links to the guest menu, with the language and table when given', () => {
    expect(menuUrl('https://armenu.app/m/', 'kadikoy-burger-lab')).toBe(
      'https://armenu.app/m/kadikoy-burger-lab',
    );
    expect(menuUrl('https://armenu.app/m/', 'kadikoy-burger-lab', { lang: 'en', table: 'Bahçe 3' })).toBe(
      'https://armenu.app/m/kadikoy-burger-lab?lang=en&table=Bah%C3%A7e+3',
    );
  });
});

describe('qrCode', () => {
  const code = qrCode('https://armenu.app/m/kadikoy-burger-lab?table=12');

  test('leaves a quiet zone and places the finder pattern inside it', () => {
    const module = (x: number, y: number) => code.modules[y]?.[x];

    expect(module(0, 0)).toBe(false);
    expect(module(1, 1)).toBe(false);
    // The top-left finder: a dark 7x7 ring around a light ring around a dark 3x3 core.
    expect(module(2, 2)).toBe(true);
    expect(module(3, 3)).toBe(false);
    expect(module(5, 5)).toBe(true);
  });

  test('draws every dark module into the path', () => {
    const darkModules = code.modules.flat().filter(Boolean).length;

    expect(code.path.match(/M/g)).toHaveLength(darkModules);
  });

  test('exports a standalone SVG with an escaped title', () => {
    const svg = qrSvgFile(code, 'Kadıköy <Burger> & Lab');

    expect(svg).toContain(`viewBox="0 0 ${code.size} ${code.size}"`);
    expect(svg).toContain('<title>Kadıköy &#60;Burger&#62; &#38; Lab</title>');
  });
});
