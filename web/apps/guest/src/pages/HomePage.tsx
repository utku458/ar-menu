import { Link } from '@tanstack/react-router';

import { env } from '../env.ts';
import { I18nProvider } from '../i18n/I18nProvider.tsx';
import { useI18n } from '../i18n/i18n-context.ts';
import { CubeIcon } from '../ui/icons.tsx';

/** What someone sees at /m/ without a restaurant: guidance, plus demo links when configured (development). */
export function HomePage() {
  return (
    <I18nProvider culture={navigator.language}>
      <HomeContent />
    </I18nProvider>
  );
}

function HomeContent() {
  const { messages } = useI18n();

  return (
    <main className="mx-auto flex min-h-dvh max-w-md flex-col justify-center gap-6 px-6 py-12">
      <CubeIcon className="size-12 text-accent" />
      <h1 className="font-serif text-3xl leading-tight text-balance">{messages.homeTitle}</h1>
      <p className="text-lg text-ink-muted">{messages.homeBody}</p>

      {env.demoMenus.length > 0 && (
        <nav aria-labelledby="demo-menus">
          <h2 id="demo-menus" className="mb-3 text-sm font-semibold tracking-wide text-ink-muted uppercase">
            {messages.demoMenus}
          </h2>
          <ul className="grid gap-2">
            {env.demoMenus.map((slug) => (
              <li key={slug}>
                <Link
                  to="/$tenant"
                  params={{ tenant: slug }}
                  search={{}}
                  className="block rounded-xl border border-line bg-surface px-4 py-3 font-medium hover:border-accent"
                >
                  {slug}
                </Link>
              </li>
            ))}
          </ul>
        </nav>
      )}
    </main>
  );
}
