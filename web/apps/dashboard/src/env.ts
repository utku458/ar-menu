function required(name: string, value: string | undefined): string {
  if (value === undefined || value.trim() === '') {
    throw new Error(`${name} is not configured. See the .env files of the dashboard.`);
  }

  return value.trim();
}

/** Build-time configuration, validated when the app starts. */
export const env = {
  apiBaseUrl: required('VITE_API_BASE_URL', import.meta.env.VITE_API_BASE_URL).replace(/\/+$/, ''),
  /** Guest menus live under this URL, followed by the business slug. It is what QR codes encode. */
  guestMenuBaseUrl: required('VITE_GUEST_MENU_BASE_URL', import.meta.env.VITE_GUEST_MENU_BASE_URL).replace(
    /\/*$/,
    '/',
  ),
} as const;
