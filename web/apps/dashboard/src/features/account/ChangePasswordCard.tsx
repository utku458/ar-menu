import { useMutation } from '@tanstack/react-query';
import { useRouter } from '@tanstack/react-router';
import { useState } from 'react';
import { Form } from 'react-aria-components';

import { type FormErrors, formErrorsFrom, noFormErrors } from '../../api/form-errors.ts';
import { ensureOk } from '../../api/result.ts';
import type { Workspace } from '../../auth/workspaces.ts';
import { useI18n } from '../../i18n/i18n-context.ts';
import { Button } from '../../ui/Button.tsx';
import { TextField } from '../../ui/fields.tsx';
import { formText } from '../../ui/form-data.ts';
import { Card } from '../../ui/layout.tsx';
import { FormAlert } from '../../ui/Modal.tsx';
import { useNotify } from '../../ui/toaster-context.ts';

/**
 * Changing one's own password, which ends every session it had — this one included. So the form signs out
 * afterwards and asks for the new password, rather than leaving a page open on a session that is already over.
 */
export function ChangePasswordCard({ workspace }: { workspace: Workspace }) {
  const { messages, describeError } = useI18n();
  const router = useRouter();
  const notify = useNotify();
  const [errors, setErrors] = useState<FormErrors>(noFormErrors);

  const change = useMutation({
    mutationFn: async (form: HTMLFormElement) => {
      const data = new FormData(form);
      ensureOk(
        await workspace.api.PUT('/api/v1/me/password', {
          body: {
            currentPassword: formText(data, 'currentPassword'),
            newPassword: formText(data, 'newPassword'),
          },
        }),
      );
    },
    onSuccess: async () => {
      notify(messages.passwordChangedSignInAgain);
      workspace.session.end();
      await router.navigate({ to: '/sign-in' });
    },
    onError: (error) => {
      setErrors(formErrorsFrom(error, { describe: describeError, networkError: messages.networkError }));
    },
  });

  return (
    <Card className="p-5">
      <h2 className="text-base font-semibold">{messages.changePassword}</h2>
      <p className="mt-1 text-sm text-ink-muted">{messages.changePasswordIntro}</p>

      <Form
        className="mt-4 flex max-w-sm flex-col gap-4"
        validationErrors={errors.fields}
        onSubmit={(event) => {
          event.preventDefault();
          change.mutate(event.currentTarget);
        }}
      >
        <TextField
          name="currentPassword"
          type="password"
          label={messages.currentPassword}
          autoComplete="current-password"
          isRequired
        />
        <TextField
          name="newPassword"
          type="password"
          label={messages.newPassword}
          description={messages.passwordHint}
          autoComplete="new-password"
          isRequired
        />
        {errors.form !== undefined && <FormAlert message={errors.form} />}
        <div>
          <Button type="submit" variant="primary" isPending={change.isPending}>
            {messages.changePassword}
          </Button>
        </div>
      </Form>
    </Card>
  );
}
