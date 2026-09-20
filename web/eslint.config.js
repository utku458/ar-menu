// @ts-check
import js from '@eslint/js';
import pluginQuery from '@tanstack/eslint-plugin-query';
import jsxA11y from 'eslint-plugin-jsx-a11y';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import { defineConfig, globalIgnores } from 'eslint/config';
import globals from 'globals';
import tseslint from 'typescript-eslint';

export default defineConfig(
  globalIgnores(['**/dist/', '**/dist-*/', '**/playwright-report/', '**/test-results/', '**/*.gen.ts']),

  {
    files: ['**/*.{ts,tsx,js}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.strictTypeChecked,
      tseslint.configs.stylisticTypeChecked,
    ],
    languageOptions: {
      parserOptions: { projectService: true, tsconfigRootDir: import.meta.dirname },
    },
    linterOptions: { reportUnusedDisableDirectives: 'error' },
    rules: {
      '@typescript-eslint/consistent-type-imports': ['error', { fixStyle: 'inline-type-imports' }],
      '@typescript-eslint/restrict-template-expressions': ['error', { allowNumber: true }],
      // Type-only callers of async functions opt out explicitly with `void`.
      '@typescript-eslint/no-floating-promises': ['error', { ignoreVoid: true }],
      // Path references are how ambient declarations (such as JSX types for custom elements) join a program.
      '@typescript-eslint/triple-slash-reference': [
        'error',
        { path: 'always', types: 'prefer-import', lib: 'always' },
      ],
      eqeqeq: ['error', 'always'],
    },
  },

  {
    files: ['apps/*/src/**/*.{ts,tsx}', 'packages/ar-viewer/src/**/*.tsx'],
    extends: [
      reactHooks.configs.flat['recommended-latest'],
      jsxA11y.flatConfigs.strict,
      pluginQuery.configs['flat/recommended'],
    ],
    languageOptions: { globals: globals.browser },
  },

  {
    files: ['apps/*/src/**/*.tsx'],
    ignores: ['**/*.test.tsx'],
    extends: [reactRefresh.configs.vite],
  },

  {
    // The guest app's startup JavaScript has a hard budget. `motion.*` components bundle every animation feature
    // up front; `m.*` components inside <LazyMotion> load them on demand. LazyMotion's `strict` mode throws at
    // runtime on a stray `motion.*`; this catches it before it ships.
    files: ['apps/guest/src/**/*.{ts,tsx}'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          paths: [
            {
              name: 'motion/react',
              importNames: ['motion'],
              message:
                'Use `m` inside <MotionFeatures>: `motion` bundles every animation feature on startup.',
            },
            { name: 'framer-motion', message: "Import from 'motion/react'." },
          ],
        },
      ],
    },
  },

  {
    files: [
      '**/*.config.{ts,js}',
      '**/vite-plugins/**',
      '**/scripts/**',
      '**/e2e/**',
      'tools/**',
      'services/**',
      'packages/model-pipeline/**',
    ],
    languageOptions: { globals: globals.node },
  },

  {
    files: ['**/*.js'],
    extends: [tseslint.configs.disableTypeChecked],
  },
);
