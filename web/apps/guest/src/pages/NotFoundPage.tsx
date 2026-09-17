import { I18nProvider } from '../i18n/I18nProvider.tsx';
import { useI18n } from '../i18n/i18n-context.ts';

export function NotFoundPage() {
  return (
    <I18nProvider culture={navigator.language}>
      <NotFoundContent />
    </I18nProvider>
  );
}

function NotFoundContent() {
  const { messages } = useI18n();

  return (
    <main className="mx-auto flex min-h-dvh max-w-md flex-col justify-center gap-4 px-6 py-12">
      <h1 className="font-serif text-3xl">{messages.pageNotFoundTitle}</h1>
      <p className="text-lg text-ink-muted">{messages.homeBody}</p>
    </main>
  );
}
