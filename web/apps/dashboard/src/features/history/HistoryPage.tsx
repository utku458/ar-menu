import type { HistoryEntryResponse } from '@armenu/api-client';
import { useInfiniteQuery, useQuery } from '@tanstack/react-query';

import { useCurrentUser, useWorkspace } from '../../app/workspace.ts';
import { useI18n } from '../../i18n/i18n-context.ts';
import { Button } from '../../ui/Button.tsx';
import { Card, PageHeader } from '../../ui/layout.tsx';
import { displayText } from '../menu/menu-editing.ts';
import { managedMenuQuery } from '../menu/menu-api.ts';
import { describeEntry, type HistoryContext } from './describe-entry.ts';
import { historyQuery } from './history-api.ts';

export function HistoryPage() {
  const { messages, language } = useI18n();
  const workspace = useWorkspace();
  const { tenant } = useCurrentUser();
  const history = useInfiniteQuery(historyQuery(workspace));
  const menu = useQuery(managedMenuQuery(workspace)).data;
  const locale = language === 'tr' ? 'tr-TR' : 'en-US';

  const context: HistoryContext = {
    messages,
    defaultCulture: tenant.defaultCulture,
    currency: tenant.currency,
    locale,
    categoryName: (id) => {
      const category = menu?.categories.find((candidate) => candidate.id === id);
      return category === undefined ? undefined : displayText(category.name, tenant.defaultCulture);
    },
  };

  // Days and times of the business, like its statistics: a change at 00:30 in Istanbul belongs to that day.
  const dayFormat = new Intl.DateTimeFormat(locale, { dateStyle: 'full', timeZone: tenant.timeZone });
  const timeFormat = new Intl.DateTimeFormat(locale, { timeStyle: 'short', timeZone: tenant.timeZone });
  const entries = history.data?.pages.flatMap((page) => page.entries) ?? [];
  const days = groupBy(entries, (entry) => dayFormat.format(new Date(entry.occurredAt)));

  return (
    <>
      <PageHeader title={messages.navHistory} description={messages.historyIntro} />

      {history.data === undefined ? (
        <p role="status" className="text-sm text-ink-muted">
          {history.isError ? messages.networkError : messages.loading}
        </p>
      ) : entries.length === 0 ? (
        <p className="max-w-3xl rounded-xl border border-dashed border-line px-6 py-10 text-center text-sm text-ink-muted">
          {messages.noHistoryYet}
        </p>
      ) : (
        <div className="grid max-w-3xl grid-cols-1 gap-6">
          {days.map(([day, dayEntries]) => (
            <Card key={day}>
              <h2 className="border-b border-line px-5 py-3 text-sm font-semibold">{day}</h2>
              <ol className="divide-y divide-line">
                {dayEntries.map((entry) => {
                  const described = describeEntry(entry, context);
                  return (
                    <li key={entry.id} className="flex gap-4 px-5 py-3">
                      <time
                        dateTime={entry.occurredAt}
                        className="w-12 shrink-0 pt-0.5 text-xs text-ink-muted tabular-nums"
                      >
                        {timeFormat.format(new Date(entry.occurredAt))}
                      </time>
                      <div className="min-w-0 flex-1 text-sm">
                        <p>
                          <span className="font-medium">{described.actor}</span> {described.summary}
                        </p>
                        {described.changes.length > 0 && (
                          <dl
                            aria-label={messages.changes}
                            className="mt-1.5 grid grid-cols-1 gap-x-3 gap-y-1 text-xs sm:grid-cols-[max-content_1fr]"
                          >
                            {described.changes.map((change) => (
                              <div key={change.field} className="contents">
                                <dt className="text-ink-muted">{change.label}</dt>
                                <dd className="break-words">
                                  <span className="text-ink-muted line-through decoration-ink-muted/60">
                                    {change.before}
                                  </span>{' '}
                                  <span aria-hidden="true">→</span>
                                  <span className="sr-only">,</span> <span>{change.after}</span>
                                </dd>
                              </div>
                            ))}
                          </dl>
                        )}
                      </div>
                    </li>
                  );
                })}
              </ol>
            </Card>
          ))}

          {history.hasNextPage && (
            <div className="flex justify-center">
              <Button
                isPending={history.isFetchingNextPage}
                onPress={() => {
                  void history.fetchNextPage();
                }}
              >
                {messages.loadMore}
              </Button>
            </div>
          )}
        </div>
      )}
    </>
  );
}

function groupBy(entries: readonly HistoryEntryResponse[], key: (entry: HistoryEntryResponse) => string) {
  const groups = new Map<string, HistoryEntryResponse[]>();
  for (const entry of entries) {
    const group = key(entry);
    groups.set(group, [...(groups.get(group) ?? []), entry]);
  }

  return [...groups];
}
