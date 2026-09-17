import type { ReactNode } from 'react';
import { useState } from 'react';
import { Form } from 'react-aria-components';

import { type FormErrors, formErrorsFrom, noFormErrors } from '../api/form-errors.ts';
import { useI18n } from '../i18n/i18n-context.ts';
import { Button } from './Button.tsx';
import { TextField } from './fields.tsx';
import { formText } from './form-data.ts';
import { FormAlert, Modal } from './Modal.tsx';

interface PasswordConfirmDialogProps {
  readonly title: string;
  readonly body: ReactNode;
  readonly confirmLabel: string;
  /** Rejects with the API's answer, which is shown next to the password or above the buttons. */
  readonly onConfirm: (password: string) => Promise<void>;
  readonly onClose: () => void;
}

/** For what cannot be undone: the API asks for the password again, so a forgotten open laptop is not enough. */
export function PasswordConfirmDialog({
  title,
  body,
  confirmLabel,
  onConfirm,
  onClose,
}: PasswordConfirmDialogProps) {
  const { messages, describeError } = useI18n();
  const [errors, setErrors] = useState<FormErrors>(noFormErrors);
  const [isPending, setIsPending] = useState(false);

  const confirm = async (form: HTMLFormElement) => {
    setIsPending(true);
    try {
      await onConfirm(formText(new FormData(form), 'password'));
    } catch (error) {
      setErrors(formErrorsFrom(error, { describe: describeError, networkError: messages.networkError }));
      setIsPending(false);
    }
  };

  return (
    <Modal
      title={title}
      role="alertdialog"
      isOpen
      onOpenChange={(isOpen) => {
        if (!isOpen) {
          onClose();
        }
      }}
    >
      <Form
        className="flex flex-col gap-5"
        validationErrors={errors.fields}
        onSubmit={(event) => {
          event.preventDefault();
          void confirm(event.currentTarget);
        }}
      >
        <div className="flex flex-col gap-2 text-sm text-ink-muted">{body}</div>
        <TextField
          name="password"
          type="password"
          label={messages.confirmWithPassword}
          autoComplete="current-password"
          isRequired
        />
        {errors.form !== undefined && <FormAlert message={errors.form} />}
        <div className="flex justify-end gap-2 border-t border-line pt-4">
          <Button onPress={onClose}>{messages.cancel}</Button>
          <Button type="submit" variant="danger" isPending={isPending}>
            {confirmLabel}
          </Button>
        </div>
      </Form>
    </Modal>
  );
}
