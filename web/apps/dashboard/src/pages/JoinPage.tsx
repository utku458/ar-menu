import { ApiError, createApiClient } from '@armenu/api-client';
import { useQuery } from '@tanstack/react-query';
import { getRouteApi, Link, useNavigate } from '@tanstack/react-router';
import { useState } from 'react';
import { Form } from 'react-aria-components';

import { type FormErrors, formErrorsFrom, noFormErrors } from '../api/form-errors.ts';
import { unwrap } from '../api/result.ts';
import { saveLastWorkspace } from '../auth/last-workspace.ts';
import { takeTokenFromUrl } from '../auth/url-token.ts';
import { workspaceFor } from '../auth/workspaces.ts';
import { env } from '../env.ts';
import { useI18n } from '../i18n/i18n-context.ts';
import { formText } from '../ui/form-data.ts';
import { Button } from '../ui/Button.tsx';
import { TextField } from '../ui/fields.tsx';
import { AuthLayout } from '../ui/layout.tsx';
import { FormAlert } from '../ui/Modal.tsx';

const route = getRouteApi('/$workspace/join');
const minimumPasswordLength = 12;

export function JoinPage() {
  const { messages, describeError } = useI18n();
  const { workspace } = route.useParams();
  const navigate = useNavigate();
  const [token] = useState(takeTokenFromUrl);
  const [api] = useState(() => createApiClient(env.apiBaseUrl));
  const [errors, setErrors] = useState<FormErrors>(noFormErrors);
  const [isPending, setIsPending] = useState(false);

  const invitation = useQuery({
    queryKey: ['invitation', workspace, token],
    queryFn: async ({ signal }) =>
      unwrap(
        await api.POST('/api/v1/tenants/{tenant}/invitations/lookup', {
          params: { path: { tenant: workspace } },
          body: { token: token ?? '' },
          signal,
        }),
      ),
    enabled: token !== undefined,
    retry: false,
    staleTime: Infinity,
  });

  const join = async (form: HTMLFormElement) => {
    const data = new FormData(form);
    setIsPending(true);
    try {
      // The response sets the refresh cookie of this business and carries the first access token.
      const grant = unwrap(
        await api.POST('/api/v1/tenants/{tenant}/invitations/accept', {
          params: { path: { tenant: workspace } },
          body: {
            token: token ?? '',
            fullName: invitation.data?.hasAccount === true ? null : formText(data, 'fullName'),
            password: formText(data, 'password'),
          },
          credentials: 'include',
        }),
      );
      workspaceFor(workspace).session.accept(grant);
      saveLastWorkspace(workspace);
      await navigate({ to: '/$workspace/menu', params: { workspace } });
    } catch (error) {
      setErrors(formErrorsFrom(error, { describe: describeError, networkError: messages.networkError }));
      setIsPending(false);
    }
  };

  if (token === undefined || invitation.isError) {
    const code = invitation.error instanceof ApiError ? invitation.error.code : undefined;
    return (
      <AuthLayout>
        <h1 className="text-2xl font-semibold tracking-tight">{messages.joinUnavailable}</h1>
        <p role="alert" className="mt-3 text-sm text-ink-muted">
          {token === undefined
            ? messages.joinLinkMissing
            : invitation.error instanceof TypeError
              ? messages.networkError
              : describeError(code)}
        </p>
        <p className="mt-8 text-sm">
          <Link
            to="/"
            search={{ choose: true }}
            className="font-medium text-accent underline-offset-4 hover:underline"
          >
            {messages.backToStart}
          </Link>
        </p>
      </AuthLayout>
    );
  }

  if (invitation.data === undefined) {
    return (
      <AuthLayout>
        <p role="status" className="text-sm text-ink-muted">
          {messages.loading}
        </p>
      </AuthLayout>
    );
  }

  const { businessName, email, role, hasAccount } = invitation.data;

  return (
    <AuthLayout>
      <h1 className="text-2xl font-semibold tracking-tight">{messages.joinTitle(businessName)}</h1>
      <p className="mt-3 text-sm">{messages.joinDetails(messages.roles[role] ?? role, email)}</p>
      <p className="mt-1 text-sm text-ink-muted">
        {hasAccount ? messages.joinWithAccount : messages.joinCreateAccount}
      </p>

      <Form
        className="mt-8 flex flex-col gap-5"
        validationErrors={errors.fields}
        onSubmit={(event) => {
          event.preventDefault();
          void join(event.currentTarget);
        }}
      >
        {/* The address is fixed by the invitation; shown for password managers and for the person to check. */}
        <TextField
          name="email"
          type="email"
          label={messages.email}
          value={email}
          autoComplete="username"
          isReadOnly
        />
        {!hasAccount && (
          <TextField name="fullName" label={messages.fullName} autoComplete="name" isRequired />
        )}
        {hasAccount ? (
          <TextField
            name="password"
            type="password"
            label={messages.password}
            autoComplete="current-password"
            isRequired
          />
        ) : (
          <TextField
            name="password"
            type="password"
            label={messages.password}
            description={messages.passwordDescription(minimumPasswordLength)}
            autoComplete="new-password"
            minLength={minimumPasswordLength}
            isRequired
          />
        )}
        {errors.form !== undefined && <FormAlert message={errors.form} />}
        <Button type="submit" variant="primary" isPending={isPending}>
          {messages.joinAction}
        </Button>
      </Form>
    </AuthLayout>
  );
}
