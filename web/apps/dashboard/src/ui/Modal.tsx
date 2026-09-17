import type { ReactNode } from 'react';
import { Dialog, Heading, Modal as AriaModal, ModalOverlay } from 'react-aria-components';

import { useI18n } from '../i18n/i18n-context.ts';
import { Button } from './Button.tsx';
import { cx } from './cx.ts';
import { CloseIcon } from './icons.tsx';

interface ModalProps {
  readonly title: string;
  readonly isOpen: boolean;
  readonly onOpenChange: (isOpen: boolean) => void;
  readonly children: ReactNode;
  readonly size?: 'md' | 'lg';
  /** For confirmations: announced as an alert, and not dismissed by clicking outside. */
  readonly role?: 'dialog' | 'alertdialog';
}

/** Focus trap, Escape, scroll lock and restored focus come from React Aria. A sheet on phones, centered on desktops. */
export function Modal({ title, isOpen, onOpenChange, children, size = 'md', role = 'dialog' }: ModalProps) {
  const { messages } = useI18n();

  return (
    <ModalOverlay
      isOpen={isOpen}
      onOpenChange={onOpenChange}
      isDismissable={role === 'dialog'}
      className="fixed inset-0 z-40 flex items-end justify-center bg-black/45 sm:items-center sm:p-6"
    >
      <AriaModal
        className={cx(
          'max-h-[92dvh] w-full overflow-y-auto rounded-t-2xl bg-surface text-ink shadow-2xl sm:rounded-2xl',
          size === 'lg' ? 'sm:max-w-3xl' : 'sm:max-w-lg',
        )}
      >
        <Dialog role={role} className="outline-none">
          <header className="sticky top-0 z-10 flex items-center justify-between gap-4 border-b border-line bg-surface px-5 py-3">
            <Heading slot="title" className="text-base font-semibold">
              {title}
            </Heading>
            <Button
              variant="ghost"
              size="icon"
              aria-label={messages.cancel}
              onPress={() => {
                onOpenChange(false);
              }}
            >
              <CloseIcon />
            </Button>
          </header>
          <div className="p-5">{children}</div>
        </Dialog>
      </AriaModal>
    </ModalOverlay>
  );
}

interface ConfirmDialogProps {
  readonly title: string;
  readonly body: string;
  readonly confirmLabel: string;
  readonly isOpen: boolean;
  readonly isPending: boolean;
  readonly error: string | undefined;
  readonly onConfirm: () => void;
  readonly onOpenChange: (isOpen: boolean) => void;
}

export function ConfirmDialog({
  title,
  body,
  confirmLabel,
  isOpen,
  isPending,
  error,
  onConfirm,
  onOpenChange,
}: ConfirmDialogProps) {
  const { messages } = useI18n();

  return (
    <Modal title={title} isOpen={isOpen} onOpenChange={onOpenChange} role="alertdialog">
      <p className="text-sm text-ink-muted">{body}</p>
      {error !== undefined && <FormAlert message={error} />}
      <div className="mt-6 flex justify-end gap-2">
        <Button
          onPress={() => {
            onOpenChange(false);
          }}
        >
          {messages.cancel}
        </Button>
        <Button variant="danger" onPress={onConfirm} isPending={isPending}>
          {confirmLabel}
        </Button>
      </div>
    </Modal>
  );
}

export function FormAlert({ message }: { message: string }) {
  return (
    <p role="alert" className="mt-4 rounded-lg bg-danger-soft px-3 py-2 text-sm text-danger">
      {message}
    </p>
  );
}
