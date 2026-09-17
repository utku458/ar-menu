import type { ReactNode } from 'react';

import { useDocumentTitle } from '../i18n/i18n-context.ts';

/** A full-page message for states where there is no menu to show. */
export function Notice({ title, body, action }: { title: string; body: string; action?: ReactNode }) {
  useDocumentTitle(title);

  return (
    <main className="mx-auto flex min-h-dvh max-w-md flex-col justify-center gap-4 px-6 py-12">
      <h1 className="font-serif text-3xl leading-tight text-balance">{title}</h1>
      <p className="text-lg text-ink-muted">{body}</p>
      {action}
    </main>
  );
}
