// Kept across restaurants: a guest who reads German at one table wants German at the next.
const storageKey = 'armenu.language';

export function readPreferredLanguage(): string | undefined {
  try {
    return localStorage.getItem(storageKey) ?? undefined;
  } catch {
    // Storage can be unavailable (private browsing, blocked cookies): the browser language still applies.
    return undefined;
  }
}

export function savePreferredLanguage(language: string): void {
  try {
    localStorage.setItem(storageKey, language);
  } catch {
    // Not remembering the choice is acceptable; failing the language switch is not.
  }
}
