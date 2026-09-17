/** @type {import('prettier').Config & import('prettier-plugin-tailwindcss').PluginOptions} */
export default {
  printWidth: 110,
  singleQuote: true,
  plugins: ['prettier-plugin-tailwindcss'],
  // Tailwind v4 is configured in CSS; the plugin reads each app's stylesheet to sort its utility classes.
  tailwindStylesheet: './apps/guest/src/styles.css',
  overrides: [
    {
      files: 'apps/dashboard/**',
      options: { tailwindStylesheet: './apps/dashboard/src/styles.css' },
    },
  ],
};
