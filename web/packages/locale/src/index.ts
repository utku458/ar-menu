// Scripts written right to left, by ISO 639 language code.
const rightToLeftLanguages = new Set(['ar', 'ckb', 'dv', 'fa', 'he', 'ps', 'sd', 'ug', 'ur', 'yi']);

export function textDirection(culture: string): 'ltr' | 'rtl' {
  const language = culture.toLowerCase().split('-')[0] ?? '';
  return rightToLeftLanguages.has(language) ? 'rtl' : 'ltr';
}

/** A language's name in that language ("Deutsch", "Türkçe"): readable for the guest looking for it. */
export function languageName(culture: string): string {
  try {
    const name = new Intl.DisplayNames([culture], { type: 'language' }).of(culture) ?? culture;
    return name.charAt(0).toLocaleUpperCase(culture) + name.slice(1);
  } catch {
    return culture;
  }
}

const priceFormats = new Map<string, Intl.NumberFormat>();

/** Local currency symbol, decimals only when a price has them: "₺385", "₺12,50". */
export function formatPrice(amount: number, currency: string, culture: string): string {
  const key = `${culture}|${currency}`;
  let format = priceFormats.get(key);
  if (format === undefined) {
    format = new Intl.NumberFormat(culture, {
      style: 'currency',
      currency,
      currencyDisplay: 'narrowSymbol',
      trailingZeroDisplay: 'stripIfInteger',
    });
    priceFormats.set(key, format);
  }

  return format.format(amount);
}
