import type { ArModelProcessingResponse, ArModelProcessingState } from '@armenu/api-client';
import { queryOptions } from '@tanstack/react-query';

import { unwrap } from '../../api/result.ts';
import type { Workspace } from '../../auth/workspaces.ts';

const pollIntervalMs = 2_000;

export function isActive(status: ArModelProcessingState | undefined): boolean {
  return status === 'queued' || status === 'processing';
}

/** The latest processing of an item, polled while it runs. `null` when the item never had one. */
export function processingQuery(workspace: Workspace, itemId: string) {
  return queryOptions({
    queryKey: ['ar-model-processing', workspace.slug, itemId],
    queryFn: async ({ signal }): Promise<ArModelProcessingResponse | null> => {
      const result = await workspace.api.GET('/api/v1/manage/menu/items/{itemId}/ar-model/processing', {
        params: { path: { itemId } },
        signal,
      });
      return result.response.status === 404 ? null : unwrap(result);
    },
    refetchInterval: (query) => (isActive(query.state.data?.status) ? pollIntervalMs : false),
  });
}

/** 18350211 → "17.5 MB", in the interface language. */
export function formatBytes(bytes: number, locale: string): string {
  const units = ['byte', 'kilobyte', 'megabyte'] as const;
  let value = bytes;
  let unit = 0;
  while (value >= 1024 && unit < units.length - 1) {
    value /= 1024;
    unit++;
  }

  return new Intl.NumberFormat(locale, {
    style: 'unit',
    unit: units[unit],
    unitDisplay: 'short',
    maximumFractionDigits: unit > 0 && value < 100 ? 1 : 0,
  }).format(value);
}

/** How much smaller the download became, as a whole percentage (93 for a 93% reduction). */
export function reductionPercent(before: number, after: number): number {
  return before <= 0 ? 0 : Math.max(0, Math.round((1 - after / before) * 100));
}

/** A bounding box in meters as centimeters on the table: "18 × 11 × 18 cm". */
export function formatDimensions(
  { width, height, depth }: { width: number; height: number; depth: number },
  locale: string,
): string {
  const centimeters = new Intl.NumberFormat(locale, { maximumFractionDigits: 1 });
  return `${[width, depth, height].map((meters) => centimeters.format(meters * 100)).join(' × ')} cm`;
}
