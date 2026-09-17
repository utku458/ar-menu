import { Link } from '@tanstack/react-router';

import { useI18n } from '../i18n/i18n-context.ts';
import { AuthLayout } from '../ui/layout.tsx';

export function NotFoundPage() {
  const { messages } = useI18n();

  return (
    <AuthLayout>
      <h1 className="text-2xl font-semibold tracking-tight">{messages.pageNotFound}</h1>
      <p className="mt-6">
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

export function RouteError({ reset }: { reset: () => void }) {
  const { messages } = useI18n();

  return (
    <AuthLayout>
      <h1 className="text-2xl font-semibold tracking-tight">{messages.networkError}</h1>
      <button
        type="button"
        onClick={reset}
        className="mt-6 h-10 rounded-lg bg-accent px-4 text-sm font-medium text-accent-ink"
      >
        {messages.tryAgain}
      </button>
    </AuthLayout>
  );
}
