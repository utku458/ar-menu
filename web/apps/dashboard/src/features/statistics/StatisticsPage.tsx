import type { DailyMenuStatistics } from '@armenu/api-client';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { getRouteApi } from '@tanstack/react-router';
import { useId, useState } from 'react';
import { ToggleButton, ToggleButtonGroup } from 'react-aria-components';

import { useWorkspace } from '../../app/workspace.ts';
import { useI18n } from '../../i18n/i18n-context.ts';
import { Card, PageHeader } from '../../ui/layout.tsx';
import {
  arRate,
  niceMax,
  type StatisticsPeriod,
  statisticsPeriods,
  statisticsQuery,
} from './statistics-api.ts';

const route = getRouteApi('/$workspace/statistics');

export function StatisticsPage() {
  const { messages, language } = useI18n();
  const workspace = useWorkspace();
  const { days } = route.useSearch();
  const navigate = route.useNavigate();
  const statistics = useQuery({ ...statisticsQuery(workspace, days), placeholderData: keepPreviousData });
  const locale = language === 'tr' ? 'tr-TR' : 'en-US';
  const number = new Intl.NumberFormat(locale, { notation: 'compact', maximumFractionDigits: 1 });
  const percent = new Intl.NumberFormat(locale, { style: 'percent' });
  const data = statistics.data;

  return (
    <>
      <PageHeader title={messages.navStatistics} description={messages.statisticsIntro} />

      {/* Filters: one row above everything they scope. */}
      <ToggleButtonGroup
        aria-label={messages.statisticsRange}
        selectionMode="single"
        disallowEmptySelection
        selectedKeys={[String(days)]}
        onSelectionChange={(keys) => {
          const [key] = [...keys];
          void navigate({ search: { days: Number(key) as StatisticsPeriod }, replace: true });
        }}
        className="mb-6 inline-flex rounded-lg border border-line bg-surface p-0.5"
      >
        {statisticsPeriods.map((period) => (
          <ToggleButton
            key={period}
            id={String(period)}
            className="cursor-default rounded-md px-3 py-1.5 text-sm font-medium text-ink-muted outline-none data-[focus-visible]:outline-2 data-[focus-visible]:outline-accent data-[selected]:bg-sunken data-[selected]:text-ink"
          >
            {messages.lastDays(period)}
          </ToggleButton>
        ))}
      </ToggleButtonGroup>

      {data === undefined ? (
        <p role="status" className="text-sm text-ink-muted">
          {statistics.isError ? messages.networkError : messages.loading}
        </p>
      ) : (
        <div aria-busy={statistics.isPlaceholderData} className="grid grid-cols-1 gap-6">
          <dl className="grid grid-cols-2 gap-3 lg:grid-cols-4">
            {(
              [
                [messages.menuViews, data.totals.menuViews],
                [messages.dishOpens, data.totals.dishOpens],
                [messages.modelViews, data.totals.modelViews],
                [messages.arStarts, data.totals.arStarts],
              ] as const
            ).map(([label, value]) => (
              <div key={label} className="rounded-xl border border-line bg-surface px-4 py-3">
                <dt className="text-sm text-ink-muted">{label}</dt>
                <dd className="mt-1 text-2xl font-semibold">{number.format(value)}</dd>
              </div>
            ))}
          </dl>

          {data.totals.menuViews + data.totals.dishOpens === 0 ? (
            <p className="rounded-xl border border-dashed border-line px-6 py-10 text-center text-sm text-ink-muted">
              {messages.noStatisticsYet}
            </p>
          ) : (
            <>
              <DailyViewsChart days={data.days} locale={locale} />
              <Card>
                <h2 className="border-b border-line px-5 py-3 text-sm font-semibold">{messages.topDishes}</h2>
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead className="text-start text-xs text-ink-muted">
                      <tr>
                        <th scope="col" className="px-5 py-2 text-start font-medium">
                          {messages.dish}
                        </th>
                        <th scope="col" className="px-3 py-2 text-end font-medium">
                          {messages.dishOpens}
                        </th>
                        <th scope="col" className="px-3 py-2 text-end font-medium">
                          {messages.modelViews}
                        </th>
                        <th scope="col" className="px-3 py-2 text-end font-medium">
                          {messages.arStarts}
                        </th>
                        <th scope="col" className="px-5 py-2 text-end font-medium">
                          {messages.arRate}
                        </th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-line tabular-nums">
                      {data.dishes.slice(0, 10).map((dish) => {
                        const rate = arRate(dish.opens, dish.arStarts);
                        return (
                          <tr key={dish.itemId}>
                            <th scope="row" className="px-5 py-2 text-start font-medium">
                              {dish.name}
                            </th>
                            <td className="px-3 py-2 text-end">{dish.opens.toLocaleString(locale)}</td>
                            <td className="px-3 py-2 text-end">{dish.modelViews.toLocaleString(locale)}</td>
                            <td className="px-3 py-2 text-end">{dish.arStarts.toLocaleString(locale)}</td>
                            <td className="px-5 py-2 text-end">
                              {rate === undefined ? '–' : percent.format(rate / 100)}
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              </Card>
            </>
          )}

          <p className="text-xs text-ink-muted">{messages.statisticsPrivacy}</p>
        </div>
      )}
    </>
  );
}

const chartHeight = 160;

/**
 * One series, so no legend: the title names it. Columns are capped at 24px with a rounded top, grow from one baseline,
 * and each one is a focusable hit target with its value; the same numbers are in a table below.
 */
function DailyViewsChart({ days, locale }: { days: readonly DailyMenuStatistics[]; locale: string }) {
  const { messages } = useI18n();
  const titleId = useId();
  const [active, setActive] = useState<number | undefined>();
  const max = niceMax(Math.max(...days.map((day) => day.menuViews)));
  const dayFormat = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short', timeZone: 'UTC' });
  const label = (day: DailyMenuStatistics) =>
    messages.dayValue(
      dayFormat.format(new Date(`${day.day}T00:00:00Z`)),
      day.menuViews.toLocaleString(locale),
    );
  const activeDay = active === undefined ? undefined : days[active];
  const tickEvery = Math.ceil(days.length / 7);

  return (
    <Card className="p-5">
      <h2 id={titleId} className="text-sm font-semibold">
        {messages.dailyMenuViews}
      </h2>
      <p aria-live="polite" className="mt-1 h-5 text-sm text-ink-muted">
        {activeDay === undefined ? '' : label(activeDay)}
      </p>

      <div className="mt-3 flex gap-2">
        <div
          className="flex flex-col justify-between text-end text-xs text-ink-muted tabular-nums"
          style={{ height: chartHeight }}
        >
          <span>{max.toLocaleString(locale)}</span>
          <span>0</span>
        </div>
        <div className="min-w-0 flex-1">
          <ul
            aria-labelledby={titleId}
            className="relative flex items-end gap-0.5 border-b border-line"
            style={{ height: chartHeight }}
            onPointerLeave={() => {
              setActive(undefined);
            }}
          >
            <li
              aria-hidden="true"
              className="pointer-events-none absolute inset-x-0 top-0 border-t border-line"
            />
            {days.map((day, index) => (
              <li key={day.day} className="flex h-full min-w-0 flex-1 items-end justify-center">
                <button
                  type="button"
                  aria-label={label(day)}
                  onPointerEnter={() => {
                    setActive(index);
                  }}
                  onFocus={() => {
                    setActive(index);
                  }}
                  onBlur={() => {
                    setActive(undefined);
                  }}
                  className="group flex h-full w-full cursor-default items-end justify-center outline-none"
                >
                  <span
                    className="block w-full max-w-6 rounded-t bg-chart group-hover:opacity-80 group-focus-visible:outline-2 group-focus-visible:outline-offset-2 group-focus-visible:outline-accent"
                    style={{
                      height: `${String((day.menuViews / max) * 100)}%`,
                      minHeight: day.menuViews > 0 ? 2 : 0,
                    }}
                  />
                </button>
              </li>
            ))}
          </ul>
          <div aria-hidden="true" className="mt-1 flex gap-0.5 text-xs text-ink-muted">
            {days.map((day, index) => (
              <span key={day.day} className="min-w-0 flex-1 truncate text-center">
                {index % tickEvery === 0 ? dayFormat.format(new Date(`${day.day}T00:00:00Z`)) : ''}
              </span>
            ))}
          </div>
        </div>
      </div>

      <details className="mt-4 text-sm">
        <summary className="cursor-default text-ink-muted">{messages.showAsTable}</summary>
        <table className="mt-2 w-full max-w-md tabular-nums">
          <thead className="text-xs text-ink-muted">
            <tr>
              <th scope="col" className="py-1 text-start font-medium">
                {messages.day}
              </th>
              <th scope="col" className="py-1 text-end font-medium">
                {messages.menuViews}
              </th>
              <th scope="col" className="py-1 text-end font-medium">
                {messages.dishOpens}
              </th>
              <th scope="col" className="py-1 text-end font-medium">
                {messages.arStarts}
              </th>
            </tr>
          </thead>
          <tbody>
            {days.map((day) => (
              <tr key={day.day} className="border-t border-line">
                <th scope="row" className="py-1 text-start font-normal">
                  {dayFormat.format(new Date(`${day.day}T00:00:00Z`))}
                </th>
                <td className="py-1 text-end">{day.menuViews.toLocaleString(locale)}</td>
                <td className="py-1 text-end">{day.dishOpens.toLocaleString(locale)}</td>
                <td className="py-1 text-end">{day.arStarts.toLocaleString(locale)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </details>
    </Card>
  );
}
