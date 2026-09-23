import { ApiError, type TeamMemberResponse } from '@armenu/api-client';
import { useState } from 'react';
import { Form } from 'react-aria-components';

import { type FormErrors, formErrorsFrom, noFormErrors } from '../../api/form-errors.ts';
import { useI18n } from '../../i18n/i18n-context.ts';
import { Button } from '../../ui/Button.tsx';
import { SelectField, TextField } from '../../ui/fields.tsx';
import { formText } from '../../ui/form-data.ts';
import { FormAlert, Modal } from '../../ui/Modal.tsx';
import { useNotify } from '../../ui/toaster-context.ts';
import { useTeamMutations } from './team-api.ts';

type AssignableRole = 'Manager' | 'Staff';

/**
 * Opens an account for someone on the team. There is no e-mail in it: the owner types a name and a password and
 * hands them over, which is what a kitchen or a till needs.
 */
export function AddMemberDialog({ onClose }: { onClose: () => void }) {
  const { messages, describeError } = useI18n();
  const { addMember } = useTeamMutations();
  const notify = useNotify();
  const [role, setRole] = useState<AssignableRole>('Staff');
  const [errors, setErrors] = useState<FormErrors>(noFormErrors);

  const submit = async (form: HTMLFormElement) => {
    const data = new FormData(form);
    const userName = formText(data, 'userName');
    try {
      await addMember.mutateAsync({
        fullName: formText(data, 'fullName'),
        userName,
        password: formText(data, 'password'),
        role,
      });
      notify(messages.memberAdded(userName));
      onClose();
    } catch (error) {
      // A taken name is the API's answer, not the field's, but it belongs next to the field that caused it.
      setErrors(
        error instanceof ApiError && error.code === 'user.user_name_taken'
          ? { fields: { userName: describeError(error.code) }, form: undefined }
          : formErrorsFrom(error, { describe: describeError, networkError: messages.networkError }),
      );
    }
  };

  return (
    <Modal
      title={messages.addMember}
      isOpen
      onOpenChange={(open) => {
        if (!open) {
          onClose();
        }
      }}
    >
      <Form
        className="flex flex-col gap-5"
        validationErrors={errors.fields}
        onSubmit={(event) => {
          event.preventDefault();
          void submit(event.currentTarget);
        }}
      >
        <TextField name="fullName" label={messages.fullName} isRequired />
        <TextField
          name="userName"
          label={messages.userName}
          description={messages.userNameHint}
          autoComplete="off"
          isRequired
        />
        <TextField
          name="password"
          type="password"
          label={messages.password}
          description={messages.passwordHint}
          autoComplete="new-password"
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
        <div className="flex justify-end gap-2">
          <Button onPress={onClose}>{messages.cancel}</Button>
          <Button type="submit" variant="primary" isPending={addMember.isPending}>
            {messages.addMember}
          </Button>
        </div>
      </Form>
    </Modal>
  );
}

/** A new password for a member who lost theirs. Every session they had ends with it. */
export function ResetMemberPasswordDialog({
  member,
  onClose,
}: {
  member: TeamMemberResponse;
  onClose: () => void;
}) {
  const { messages, describeError } = useI18n();
  const { setMemberPassword } = useTeamMutations();
  const notify = useNotify();
  const [errors, setErrors] = useState<FormErrors>(noFormErrors);

  const submit = async (form: HTMLFormElement) => {
    try {
      await setMemberPassword.mutateAsync({
        membershipId: member.id,
        password: formText(new FormData(form), 'password'),
      });
      notify(messages.memberPasswordReset(member.fullName));
      onClose();
    } catch (error) {
      setErrors(formErrorsFrom(error, { describe: describeError, networkError: messages.networkError }));
    }
  };

  return (
    <Modal
      title={messages.resetMemberPassword}
      isOpen
      onOpenChange={(open) => {
        if (!open) {
          onClose();
        }
      }}
    >
      <Form
        className="flex flex-col gap-5"
        validationErrors={errors.fields}
        onSubmit={(event) => {
          event.preventDefault();
          void submit(event.currentTarget);
        }}
      >
        <p className="text-sm text-ink-muted">
          {messages.resetMemberPasswordIntro(member.fullName, member.userName ?? member.email)}
        </p>
        <TextField
          name="password"
          type="password"
          label={messages.newPassword}
          description={messages.passwordHint}
          autoComplete="new-password"
          isRequired
        />
        {errors.form !== undefined && <FormAlert message={errors.form} />}
        <div className="flex justify-end gap-2">
          <Button onPress={onClose}>{messages.cancel}</Button>
          <Button type="submit" variant="primary" isPending={setMemberPassword.isPending}>
            {messages.setNewPassword}
          </Button>
        </div>
      </Form>
    </Modal>
  );
}
