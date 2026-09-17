import { getRouteApi, Link, useRouter } from '@tanstack/react-router';
import { useState } from 'react';
import { Form } from 'react-aria-components';

import { type FormErrors, formErrorsFrom, noFormErrors } from '../api/form-errors.ts';
import { saveLastWorkspace } from '../auth/last-workspace.ts';
import { safeRedirect } from '../auth/slug.ts';
import { workspaceFor } from '../auth/workspaces.ts';
import { useI18n } from '../i18n/i18n-context.ts';
import { formText } from '../ui/form-data.ts';
import { Button } from '../ui/Button.tsx';
import { TextField } from '../ui/fields.tsx';
import { AuthLayout } from '../ui/layout.tsx';
import { FormAlert } from '../ui/Modal.tsx';

const route = getRouteApi('/$workspace/sign-in');

export function SignInPage() {
  const { messages, describeError } = useI18n();
  const { workspace } = route.useParams();
  const { redirect, ended } = route.useSearch();
  const router = useRouter();
  const [errors, setErrors] = useState<FormErrors>(noFormErrors);
  const [isPending, setIsPending] = useState(false);

  const signIn = async (form: HTMLFormElement) => {
    const data = new FormData(form);
    setIsPending(true);
    try {
      await workspaceFor(workspace).session.signIn(formText(data, 'email'), formText(data, 'password'));
      saveLastWorkspace(workspace);
      const target = safeRedirect(redirect);
      if (target === undefined) {
        await router.navigate({ to: '/$workspace/menu', params: { workspace } });
      } else {
        router.history.push(target);
      }
    } catch (error) {
      setErrors(formErrorsFrom(error, { describe: describeError, networkError: messages.networkError }));
    } finally {
      setIsPending(false);
    }
  };

  return (
    <AuthLayout>
      <h1 className="text-2xl font-semibold tracking-tight">{messages.signInTitle(workspace)}</h1>
      {ended === true && (
        <p role="status" className="mt-3 rounded-lg bg-accent-soft px-3 py-2 text-sm">
          {messages.sessionEnded}
        </p>
      )}

      <Form
        className="mt-8 flex flex-col gap-5"
        validationErrors={errors.fields}
        onSubmit={(event) => {
          event.preventDefault();
          void signIn(event.currentTarget);
        }}
      >
        <TextField name="email" type="email" label={messages.email} autoComplete="username" isRequired />
        <TextField
          name="password"
          type="password"
          label={messages.password}
          autoComplete="current-password"
          isRequired
        />
        {errors.form !== undefined && <FormAlert message={errors.form} />}
        <Button type="submit" variant="primary" isPending={isPending}>
          {messages.signIn}
        </Button>
      </Form>

      <p className="mt-4 text-sm">
        <Link
          to="/forgot-password"
          search={{ workspace }}
          className="font-medium text-accent underline-offset-4 hover:underline"
        >
          {messages.forgotPassword}
        </Link>
      </p>

      <p className="mt-8 text-sm">
        <Link
          to="/"
          search={{ choose: true }}
          className="font-medium text-accent underline-offset-4 hover:underline"
        >
          {messages.otherBusiness}
        </Link>
      </p>
    </AuthLayout>
  );
}
