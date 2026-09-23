import { ApiError, createApiClient } from '@armenu/api-client';
import { useMutation } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { useEffect, useState } from 'react';

import { ensureOk } from '../api/result.ts';
import { readLastWorkspace } from '../auth/last-workspace.ts';
import { takeTokenFromUrl } from '../auth/url-token.ts';
import { env } from '../env.ts';
import { useI18n } from '../i18n/i18n-context.ts';
import { AuthLayout } from '../ui/layout.tsx';

/** Confirms the address as soon as the link opens; open dashboard tabs pick it up when they are looked at again. */
export function VerifyEmailPage() {
  const { messages, describeError } = useI18n();
  const [token] = useState(takeTokenFromUrl);
  const workspace = readLastWorkspace();

  const verify = useMutation({
    mutationFn: async (value: string) => {
      ensureOk(
        await createApiClient(env.apiBaseUrl).POST('/api/v1/auth/email-verification/confirm', {
          body: { token: value },
        }),
      );
    },
  });

  const { mutate } = verify;
  useEffect(() => {
    if (token !== undefined) {
      mutate(token);
    }
  }, [token, mutate]);

  const linkClass = 'font-medium text-accent underline-offset-4 hover:underline';

  return (
    <AuthLayout>
      <h1 className="text-2xl font-semibold tracking-tight">{messages.verifyEmailTitle}</h1>
      {token === undefined || verify.isError ? (
        <p role="alert" className="mt-3 text-sm text-ink-muted">
          {token === undefined
            ? messages.linkMissing
            : verify.error instanceof ApiError
              ? describeError(verify.error.code)
              : messages.networkError}
        </p>
      ) : verify.isSuccess ? (
        <p role="status" className="mt-4 rounded-lg bg-accent-soft px-3 py-2 text-sm">
          {messages.emailVerified}
        </p>
      ) : (
        <p role="status" className="mt-3 text-sm text-ink-muted">
          {messages.loading}
        </p>
      )}
      <p className="mt-8 text-sm">
        {workspace === undefined ? (
          <Link to="/sign-in" className={linkClass}>
            {messages.continueToDashboard}
          </Link>
        ) : (
          <Link to="/$workspace/menu" params={{ workspace }} className={linkClass}>
            {messages.continueToDashboard}
          </Link>
        )}
      </p>
    </AuthLayout>
  );
}
