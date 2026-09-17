// Only the business slug is remembered, which is public (it is in the menu link); never tokens.
const storageKey = 'armenu.dashboard.workspace';

export function readLastWorkspace(): string | undefined {
  try {
    return localStorage.getItem(storageKey) ?? undefined;
  } catch {
    return undefined;
  }
}

export function saveLastWorkspace(slug: string): void {
  try {
    localStorage.setItem(storageKey, slug);
  } catch {
    // Not remembering the business is acceptable.
  }
}

export function forgetLastWorkspace(): void {
  try {
    localStorage.removeItem(storageKey);
  } catch {
    // Nothing to forget.
  }
}
