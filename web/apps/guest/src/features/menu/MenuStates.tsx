import { ApiError } from '@armenu/api-client';

import { useI18n } from '../../i18n/i18n-context.ts';
import { Notice } from '../../ui/Notice.tsx';

/** Placeholder with the shape of a menu, so nothing jumps when the real one arrives. */
export function MenuSkeleton() {
  const { messages } = useI18n();

  return (
    <div className="mx-auto max-w-2xl px-4 pt-10">
      <p role="status" className="sr-only">
        {messages.loadingMenu}
      </p>
      <div aria-hidden="true" className="animate-pulse">
        <div className="mx-1 h-3 w-16 rounded-sm bg-line" />
        <div className="mx-1 mt-3 h-8 w-2/3 rounded-md bg-line" />
        <div className="mt-8 flex gap-2">
          {[0, 1, 2].map((chip) => (
            <div key={chip} className="h-9 w-24 rounded-full bg-line" />
          ))}
        </div>
        {[0, 1].map((section) => (
          <div key={section} className="mt-10">
            <div className="mx-1 h-6 w-40 rounded-md bg-line" />
            <div className="mt-4 grid gap-3">
              {[0, 1, 2].map((row) => (
                <div key={row} className="h-28 rounded-2xl border border-line bg-surface" />
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

interface MenuLoadErrorProps {
  readonly error: Error;
  readonly onRetry: () => void;
  readonly isRetrying: boolean;
}

export function MenuLoadError({ error, onRetry, isRetrying }: MenuLoadErrorProps) {
  const { messages } = useI18n();
  const status = error instanceof ApiError ? error.status : undefined;

  if (status === 404) {
    return <Notice title={messages.menuNotFoundTitle} body={messages.menuNotFoundBody} />;
  }

  return (
    <Notice
      title={messages.loadFailedTitle}
      body={status === 429 ? messages.rateLimitedBody : messages.loadFailedBody}
      action={
        <button
          type="button"
          onClick={onRetry}
          disabled={isRetrying}
          className="h-12 w-fit rounded-full bg-accent px-6 font-semibold text-accent-ink disabled:opacity-60"
        >
          {messages.tryAgain}
        </button>
      }
    />
  );
}
