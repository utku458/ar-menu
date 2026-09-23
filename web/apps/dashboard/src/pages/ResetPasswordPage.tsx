import { createApiClient } from '@armenu/api-client';
import { useState } from 'react';
import { Form } from 'react-aria-components';

import { type FormErrors, formErrorsFrom, noFormErrors } from '../api/form-errors.ts';
import { ensureOk } from '../api/result.ts';
import { takeTokenFromUrl } from '../auth/url-token.ts';
import { env } from '../env.ts';
import { useI18n } from '../i18n/i18n-context.ts';
import { formText } from '../ui/form-data.ts';
import { Button } from '../ui/Button.tsx';
import { TextField } from '../ui/fields.tsx';
import { AuthLayout } from '../ui/layout.tsx';
import { FormAlert } from '../ui/Modal.tsx';
import { BackLink } from './ForgotPasswordPage.tsx';

const minimumPasswordLength = 12;

export function ResetPasswordPage() {
  const { messages, describeError } = useI18n();
  const [token] = useState(takeTokenFromUrl);
  const [isDone, setIsDone] = useState(false);
  const [errors, setErrors] = useState<FormErrors>(noFormErrors);
  const [isPending, setIsPending] = useState(false);

  const reset = async (form: HTMLFormElement) => {
    setIsPending(true);
    try {
      ensureOk(
        await createApiClient(env.apiBaseUrl).POST('/api/v1/auth/password-reset/confirm', {
          body: { token: token ?? '', password: formText(new FormData(form), 'password') },
        }),
      );
      setIsDone(true);
    } catch (error) {
      setErrors(formErrorsFrom(error, { describe: describeError, networkError: messages.networkError }));
    } finally {
      setIsPending(false);
    }
  };

  return (
    <AuthLayout>
      <h1 className="text-2xl font-semibold tracking-tight">
        {token === undefined ? messages.linkUnavailable : messages.resetPasswordTitle}
      </h1>
      {token === undefined ? (
        <p role="alert" className="mt-3 text-sm text-ink-muted">
          {messages.linkMissing}
        </p>
      ) : isDone ? (
        <p role="status" className="mt-4 rounded-lg bg-accent-soft px-3 py-2 text-sm">
          {messages.passwordChanged}
        </p>
      ) : (
        <Form
          className="mt-8 flex flex-col gap-5"
          validationErrors={errors.fields}
          onSubmit={(event) => {
            event.preventDefault();
            void reset(event.currentTarget);
          }}
        >
          <TextField
            name="password"
            type="password"
            label={messages.newPassword}
            description={messages.passwordDescription(minimumPasswordLength)}
            autoComplete="new-password"
            minLength={minimumPasswordLength}
            isRequired
          />
          {errors.form !== undefined && <FormAlert message={errors.form} />}
          <Button type="submit" variant="primary" isPending={isPending}>
            {messages.setNewPassword}
          </Button>
        </Form>
      )}
      <p className="mt-8 text-sm">
        <BackLink />
      </p>
    </AuthLayout>
  );
}
