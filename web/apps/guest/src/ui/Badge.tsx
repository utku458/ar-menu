import type { ReactNode } from 'react';

import { cx } from './cx.ts';

export function Badge({ tone, children }: { tone: 'accent' | 'muted'; children: ReactNode }) {
  return (
    <span
      className={cx(
        'inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-semibold',
        tone === 'accent' ? 'bg-accent-soft text-accent' : 'bg-stage text-ink-muted',
      )}
    >
      {children}
    </span>
  );
}
