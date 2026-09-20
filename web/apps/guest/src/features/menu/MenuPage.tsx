import type { PublicMenuItemResponse, PublicMenuResponse } from '@armenu/api-client';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { getRouteApi, useLocation, useRouter } from '@tanstack/react-router';
import { lazy, Suspense, useEffect, useMemo, useState } from 'react';

import { I18nProvider } from '../../i18n/I18nProvider.tsx';
import { MenuEventsContext, trackerFor } from '../analytics/menu-events-context.ts';
import { useDocumentTitle, useI18n } from '../../i18n/i18n-context.ts';
import { applyBranding, clearBranding } from './branding.ts';
import { CategoryNav } from './CategoryNav.tsx';
import { savePreferredLanguage } from './language-preference.ts';
import { loadItemSheet, preloadItemSheetWhenIdle } from './load-item-sheet.ts';
import { LanguageSwitcher } from './LanguageSwitcher.tsx';
import { filterMenu, noFilter } from './menu-filter.ts';
import { MenuFilterBar } from './MenuFilterBar.tsx';
import { publicMenuQuery } from './menu-query.ts';
import { toMenuRequest } from './menu-request.ts';
import { MenuSection } from './MenuSection.tsx';
import { MenuLoadError, MenuSkeleton } from './MenuStates.tsx';

const route = getRouteApi('/$tenant');

const ItemSheet = lazy(async () => ({ default: (await loadItemSheet()).ItemSheet }));

export function MenuPage() {
  const { tenant } = route.useParams();
  const { lang, item, table } = route.useSearch();
  const request = toMenuRequest(tenant, lang);
  // While another language loads, the current one stays on screen instead of a skeleton.
  const menu = useQuery({ ...publicMenuQuery(request), placeholderData: keepPreviousData });

  const culture = menu.data?.culture ?? request.lang ?? navigator.language;

  return (
    <I18nProvider culture={culture}>
      {menu.data !== undefined ? (
        <Menu menu={menu.data} openItemId={item} table={table} isChangingLanguage={menu.isPlaceholderData} />
      ) : menu.isError ? (
        <MenuLoadError error={menu.error} onRetry={() => void menu.refetch()} isRetrying={menu.isFetching} />
      ) : (
        <MenuSkeleton />
      )}
    </I18nProvider>
  );
}

interface MenuProps {
  readonly menu: PublicMenuResponse;
  readonly openItemId: string | undefined;
  readonly table: string | undefined;
  readonly isChangingLanguage: boolean;
}

function Menu({ menu, openItemId, table, isChangingLanguage }: MenuProps) {
  const { messages } = useI18n();
  const navigate = route.useNavigate();
  const router = useRouter();
  const openedFromMenu = useLocation({ select: (location) => location.state.openedFromMenu === true });

  useDocumentTitle(messages.pageTitle(menu.tenant.name));

  const [filter, setFilter] = useState(noFilter);
  // Kept across language switches: a guest avoiding nuts still does after reading the menu in English.
  const { categories, culture } = menu;
  const filtered = useMemo(() => filterMenu(categories, filter, culture), [categories, culture, filter]);

  const { accentColor, onAccentColor } = menu.tenant;
  useEffect(() => {
    applyBranding(document.documentElement, { accentColor, onAccentColor });
    return () => {
      clearBranding(document.documentElement);
    };
  }, [accentColor, onAccentColor]);

  // The sheet chunk arrives in idle time; see load-item-sheet.ts.
  useEffect(() => preloadItemSheetWhenIdle(), []);

  // Mounted the first time a dish opens and kept afterwards, so closing plays the sheet's exit animation instead of
  // unmounting it mid-flight. Before that first open there is nothing to render and nothing to load.
  const [isSheetMounted, setIsSheetMounted] = useState(openItemId !== undefined);
  if (openItemId !== undefined && !isSheetMounted) {
    setIsSheetMounted(true);
  }

  const tracker = trackerFor(menu.tenant.slug);
  const openItemExists = findItem(menu, openItemId) !== undefined;
  useEffect(() => {
    tracker.track({ type: 'menu_viewed' });
  }, [tracker]);
  useEffect(() => {
    if (openItemId !== undefined && openItemExists) {
      tracker.track({ type: 'dish_opened', itemId: openItemId });
    }
  }, [tracker, openItemId, openItemExists]);

  const changeLanguage = (culture: string) => {
    savePreferredLanguage(culture);
    void navigate({ search: (search) => ({ ...search, lang: culture }), replace: true, resetScroll: false });
  };

  const openItem = (itemId: string) => {
    void navigate({
      search: (search) => ({ ...search, item: itemId }),
      state: { openedFromMenu: true },
      resetScroll: false,
    });
  };

  // Opened from the menu: going back removes the entry the sheet added. Opened from a link: no menu entry to return to.
  const closeItem = () => {
    if (openedFromMenu) {
      router.history.back();
    } else {
      void navigate({
        search: (search) => ({ ...search, item: undefined }),
        replace: true,
        resetScroll: false,
      });
    }
  };

  return (
    <MenuEventsContext
      value={(event) => {
        tracker.track(event);
      }}
    >
      {/* The business's colour, as a band no text sits on. */}
      <div aria-hidden="true" className="h-1 bg-brand" />

      <header className="mx-auto flex max-w-2xl items-end justify-between gap-4 px-5 pt-8 pb-5">
        <div className="flex min-w-0 items-center gap-4">
          {menu.tenant.logoUrl !== null && (
            <img
              src={menu.tenant.logoUrl}
              alt=""
              className="size-14 shrink-0 rounded-xl bg-surface object-contain ring-1 ring-line"
            />
          )}
          <div className="min-w-0">
            <p className="flex items-center gap-2 text-xs font-semibold tracking-[0.2em] text-accent uppercase rtl:tracking-normal">
              {messages.menu}
              {table !== undefined && (
                <span className="rounded-full bg-brand px-2 py-0.5 tracking-normal text-brand-ink normal-case">
                  {messages.table(table)}
                </span>
              )}
            </p>
            <h1 className="mt-1 font-serif text-[2rem] leading-tight text-balance">{menu.tenant.name}</h1>
          </div>
        </div>
        {menu.tenant.supportedCultures.length > 1 && (
          <LanguageSwitcher
            cultures={menu.tenant.supportedCultures}
            value={menu.culture}
            onChange={changeLanguage}
            isBusy={isChangingLanguage}
          />
        )}
      </header>

      {menu.categories.length > 0 && <MenuFilterBar filter={filter} result={filtered} onChange={setFilter} />}

      <CategoryNav categories={filtered.categories} />

      <main aria-busy={isChangingLanguage} className="mx-auto max-w-2xl px-4 pb-12">
        {menu.categories.length === 0 ? (
          <p className="px-1 pt-8 text-ink-muted">{messages.emptyMenu}</p>
        ) : (
          filtered.categories.map((category, index) => (
            <MenuSection
              key={category.id}
              category={category}
              currency={menu.tenant.currency}
              onOpenItem={openItem}
              isFirst={index === 0}
            />
          ))
        )}
      </main>

      <footer className="pb-10 text-center text-xs text-ink-muted">{messages.poweredBy}</footer>

      {isSheetMounted && (
        // No fallback: the chunk is normally preloaded, and a sheet that appears a moment late beats a spinner.
        <Suspense fallback={null}>
          <ItemSheet item={findItem(menu, openItemId)} currency={menu.tenant.currency} onClose={closeItem} />
        </Suspense>
      )}
    </MenuEventsContext>
  );
}

function findItem(menu: PublicMenuResponse, itemId: string | undefined): PublicMenuItemResponse | undefined {
  if (itemId === undefined) {
    return undefined;
  }

  for (const category of menu.categories) {
    const item = category.items.find((candidate) => candidate.id === itemId);
    if (item !== undefined) {
      return item;
    }
  }

  return undefined;
}
