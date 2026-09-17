import { ApiError } from '@armenu/api-client';
import { useState } from 'react';
import { Form } from 'react-aria-components';

import { type FormErrors, formErrorsFrom, noFormErrors } from '../../api/form-errors.ts';
import { useI18n } from '../../i18n/i18n-context.ts';
import { formText } from '../../ui/form-data.ts';
import { Button } from '../../ui/Button.tsx';
import { SelectField, TextField } from '../../ui/fields.tsx';
import { FormAlert, Modal } from '../../ui/Modal.tsx';
import { useNotify } from '../../ui/toaster-context.ts';
import { useTeamMutations } from './team-api.ts';

type InvitableRole = 'Manager' | 'Staff';

// Found by the API, not by field validation; they still belong next to the address.
const emailConflicts = new Set(['invitation.already_pending', 'membership.already_member']);

export function InviteDialog({ onClose }: { onClose: () => void }) {
  const { messages, describeError } = useI18n();
  const { invite } = useTeamMutations();
  const notify = useNotify();
  const [role, setRole] = useState<InvitableRole>('Staff');
  const [errors, setErrors] = useState<FormErrors>(noFormErrors);

  const send = async (form: HTMLFormElement) => {
    const email = formText(new FormData(form), 'email');
    try {
      const sent = await invite.mutateAsync({ email, role });
      if (sent.emailSent) {
        notify(messages.invitationSent(email));
      } else {
        notify(messages.invitationNotEmailed, 'error');
      }
      onClose();
    } catch (error) {
      setErrors(
        error instanceof ApiError && emailConflicts.has(error.code ?? '')
          ? { fields: { email: describeError(error.code) }, form: undefined }
          : formErrorsFrom(error, { describe: describeError, networkError: messages.networkError }),
      );
    }
  };

  return (
    <Modal
      title={messages.inviteTitle}
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
          void send(event.currentTarget);
        }}
      >
        <TextField
          name="email"
          type="email"
          label={messages.email}
          description={messages.inviteEmailDescription}
          autoComplete="off"
          isRequired
        />
        <SelectField
          name="role"
          label={messages.role}
          description={messages.roleDescriptions[role]}
          options={(['Staff', 'Manager'] as const).map((id) => ({ id, label: messages.roles[id] ?? id }))}
          value={role}
          onChange={(key) => {
            if (key === 'Manager' || key === 'Staff') {
              setRole(key);
            }
          }}
          isRequired
        />
        {errors.form !== undefined && <FormAlert message={errors.form} />}
        <div className="flex justify-end gap-2 border-t border-line pt-4">
          <Button onPress={onClose}>{messages.cancel}</Button>
          <Button type="submit" variant="primary" isPending={invite.isPending}>
            {messages.sendInvitation}
          </Button>
        </div>
      </Form>
    </Modal>
  );
}
