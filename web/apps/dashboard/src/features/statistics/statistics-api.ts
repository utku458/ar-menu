import { queryOptions } from '@tanstack/react-query';

import { unwrap } from '../../api/result.ts';
import type { Workspace } from '../../auth/workspaces.ts';

export const statisticsPeriods = [7, 30, 90] as const;
export type StatisticsPeriod = (typeof statisticsPeriods)[number];

export function statisticsQuery(workspace: Workspace, days: StatisticsPeriod) {
  return queryOptions({
    queryKey: ['statistics', workspace.slug, days],
    queryFn: async ({ signal }) =>
      unwrap(await workspace.api.GET('/api/v1/manage/statistics', { params: { query: { days } }, signal })),
    // Counters move while guests browse; a minute old is fresh enough for a report.
    staleTime: 60_000,
  });
}

/** Share of dish openings that went on to AR, rounded; undefined when nobody opened the dish. */
export function arRate(opens: number, arStarts: number): number | undefined {
  return opens === 0 ? undefined : Math.round((Math.min(arStarts, opens) / opens) * 100);
}

/** A clean upper bound for the axis, at least the largest value and close above it, so the columns use the height. */
export function niceMax(value: number): number {
  if (value <= 0) {
    return 1;
  }

  const power = 10 ** Math.floor(Math.log10(value));
  return (
    [1, 1.5, 2, 2.5, 3, 4, 5, 6, 8, 10].map((step) => step * power).find((candidate) => candidate >= value) ??
    10 * power
  );
}
