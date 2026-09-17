import { encode } from 'uqr';

export interface MenuLinkOptions {
  /** Opens the menu in this language instead of the guest's phone language. */
  readonly lang?: string | undefined;
  /** Shown to the guest as the table the code stands on. */
  readonly table?: string | undefined;
}

/** The guest menu URL a QR code encodes, e.g. https://armenu.app/m/kadikoy-burger-lab?table=12. */
export function menuUrl(baseUrl: string, slug: string, { lang, table }: MenuLinkOptions = {}): string {
  const url = new URL(encodeURIComponent(slug), baseUrl);
  if (lang !== undefined) {
    url.searchParams.set('lang', lang);
  }
  if (table !== undefined) {
    url.searchParams.set('table', table);
  }

  return url.toString();
}

export interface QrCode {
  /** Modules per side, quiet zone included. */
  readonly size: number;
  /** One SVG path of all dark modules: a crisp code at any print size. */
  readonly path: string;
  readonly modules: readonly (readonly boolean[])[];
}

/** Error correction level M still scans with a smudge or a crease in a printed card. */
export function qrCode(text: string): QrCode {
  const { data, size } = encode(text, { ecc: 'M', border: 2 });
  let path = '';
  data.forEach((row, y) => {
    row.forEach((dark, x) => {
      if (dark) {
        path += `M${x} ${y}h1v1h-1z`;
      }
    });
  });

  return { size, path, modules: data };
}

/** A standalone SVG file, for printers and design tools. */
export function qrSvgFile(code: QrCode, title: string): string {
  const escaped = title.replace(/[<>&"]/g, (character) => `&#${character.charCodeAt(0)};`);
  return (
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${code.size} ${code.size}" shape-rendering="crispEdges">` +
    `<title>${escaped}</title><rect width="${code.size}" height="${code.size}" fill="#fff"/>` +
    `<path d="${code.path}" fill="#000"/></svg>`
  );
}

/** A PNG with whole-pixel modules, so it stays sharp when scaled by an integer. */
export function qrPngFile(code: QrCode, pixelsPerModule = 24): Promise<Blob> {
  const canvas = document.createElement('canvas');
  canvas.width = canvas.height = code.size * pixelsPerModule;
  const context = canvas.getContext('2d');
  if (context === null) {
    return Promise.reject(new Error('Canvas 2D is not available.'));
  }

  context.fillStyle = '#fff';
  context.fillRect(0, 0, canvas.width, canvas.height);
  context.fillStyle = '#000';
  code.modules.forEach((row, y) => {
    row.forEach((dark, x) => {
      if (dark) {
        context.fillRect(x * pixelsPerModule, y * pixelsPerModule, pixelsPerModule, pixelsPerModule);
      }
    });
  });

  return new Promise((resolve, reject) => {
    canvas.toBlob((blob) => {
      if (blob === null) {
        reject(new Error('The QR code could not be rendered.'));
      } else {
        resolve(blob);
      }
    }, 'image/png');
  });
}
