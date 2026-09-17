import { createContext, use } from 'react';

export interface Toast {
  readonly id: number;
  readonly message: string;
  readonly tone: 'success' | 'error';
}

export type Notify = (message: string, tone?: Toast['tone']) => void;

export const ToasterContext = createContext<Notify | undefined>(undefined);

/** Short confirmations and background failures, announced to screen readers. */
export function useNotify(): Notify {
  const notify = use(ToasterContext);
  if (notify === undefined) {
    throw new Error('useNotify must be used inside a Toaster.');
  }

  return notify;
}
