import { createApiClient } from '@armenu/api-client';
import { getRouteApi, Link } from '@tanstack/react-router';
import { useState } from 'react';
import { Form } from 'react-aria-components';

import { type FormErrors, formErrorsFrom, noFormErrors } from '../api/form-errors.ts';
import { ensureOk } from '../api/result.ts';
import { env } from '../env.ts';
import { useI18n } from '../i18n/i18n-context.ts';
import { formText } from '../ui/form-data.ts';
import { Button } from '../ui/Button.tsx';
import { TextField } from '../ui/fields.tsx';
import { AuthLayout } from '../ui/layout.tsx';
import { FormAlert } from '../ui/Modal.tsx';

const route = getRouteApi('/forgot-password');

export function ForgotPasswordPage() {
  const { messages, language, describeError } = useI18n();
  const { workspace } = route.useSearch();
  const [sentTo, setSentTo] = useState<string | undefined>();
  const [errors, setErrors] = useState<FormErrors>(noFormErrors);
  const [isPending, setIsPending] = useState(false);

  const send = async (form: HTMLFormElement) => {
    const email = formText(new FormData(form), 'email');
    setIsPending(true);
    try {
      ensureOk(
        await createApiClient(env.apiBaseUrl).POST('/api/v1/auth/password-reset', {
          body: { email, language },
        }),
      );
      setSentTo(email);
    } catch (error) {
      setErrors(formErrorsFrom(error, { describe: describeError, networkError: messages.networkError }));
    } finally {
      setIsPending(false);
    }
  };

  return (
    <AuthLayout>
      <h1 className="text-2xl font-semibold tracking-tight">{messages.forgotPasswordTitle}</h1>
      {sentTo === undefined ? (
        <>
          <p className="mt-3 text-sm text-ink-muted">{messages.forgotPasswordIntro}</p>
          <Form
            className="mt-8 flex flex-col gap-5"
            validationErrors={errors.fields}
            onSubmit={(event) => {
              event.preventDefault();
              void send(event.currentTarget);
            }}
          >
            <TextField name="email" type="email" label={messages.email} autoComplete="username" isRequired />
            {errors.form !== undefined && <FormAlert message={errors.form} />}
            <Button type="submit" variant="primary" isPending={isPending}>
              {messages.sendResetLink}
            </Button>
          </Form>
        </>
      ) : (
        // The same message whether or not an account exists: the page must not reveal which addresses have one.
        <p role="status" className="mt-4 rounded-lg bg-accent-soft px-3 py-2 text-sm">
          {messages.resetLinkSent(sentTo)}
        </p>
      )}
      <p className="mt-8 text-sm">
        <BackLink workspace={workspace} />
      </p>
    </AuthLayout>
  );
}

export function BackLink({ workspace }: { workspace: string | undefined }) {
  const { messages } = useI18n();
  const className = 'font-medium text-accent underline-offset-4 hover:underline';

  return workspace === undefined ? (
    <Link to="/" search={{ choose: true }} className={className}>
      {messages.backToSignIn}
    </Link>
  ) : (
    <Link to="/$workspace/sign-in" params={{ workspace }} className={className}>
      {messages.backToSignIn}
    </Link>
  );
}
