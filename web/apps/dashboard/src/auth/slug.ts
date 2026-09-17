/** A menu address suggestion from a business name: "Kadıköy Çiğ Köfte" → "kadikoy-cig-kofte". */
export function slugify(name: string): string {
  return name
    .toLocaleLowerCase('tr')
    .replace(/ı/g, 'i')
    .normalize('NFKD')
    .replace(/[̀-ͯ]/g, '')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 63)
    .replace(/-+$/, '');
}

/** Only paths inside the dashboard are followed after signing in, never another site. */
export function safeRedirect(target: string | undefined): string | undefined {
  return target !== undefined && target.startsWith('/') && !target.startsWith('//') ? target : undefined;
}
