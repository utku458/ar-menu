function required(name: string, value: string | undefined): string {
  if (value === undefined || value.trim() === '') {
    throw new Error(`${name} is not configured. See the .env files of the guest app.`);
  }

  return value.trim().replace(/\/+$/, '');
}

/** Build-time configuration, validated when the app starts. */
export const env = {
  apiBaseUrl: required('VITE_API_BASE_URL', import.meta.env.VITE_API_BASE_URL),
  demoMenus: (import.meta.env.VITE_DEMO_MENUS ?? '')
    .split(',')
    .map((slug) => slug.trim())
    .filter((slug) => slug !== ''),
} as const;
