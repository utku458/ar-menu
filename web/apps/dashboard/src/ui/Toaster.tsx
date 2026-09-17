import { type ReactNode, useState } from 'react';

import { cx } from './cx.ts';
import { type Notify, type Toast, ToasterContext } from './toaster-context.ts';

const visibleMs = 4_000;
let nextId = 1;

export function Toaster({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<readonly Toast[]>([]);

  const notify: Notify = (message, tone = 'success') => {
    const toast = { id: nextId++, message, tone };
    setToasts((current) => [...current, toast]);
    setTimeout(() => {
      setToasts((current) => current.filter((candidate) => candidate.id !== toast.id));
    }, visibleMs);
  };

  return (
    <ToasterContext value={notify}>
      {children}
      {/* Always mounted, so screen readers announce messages added later. */}
      <div
        data-print-hidden
        aria-live="polite"
        className="pointer-events-none fixed inset-x-0 bottom-4 z-50 flex flex-col items-center gap-2 px-4"
      >
        {toasts.map((toast) => (
          <p
            key={toast.id}
            role={toast.tone === 'error' ? 'alert' : 'status'}
            className={cx(
              'rounded-lg px-4 py-2.5 text-sm font-medium shadow-lg',
              toast.tone === 'error' ? 'bg-danger text-white' : 'bg-ink text-canvas',
            )}
          >
            {toast.message}
          </p>
        ))}
      </div>
    </ToasterContext>
  );
}
