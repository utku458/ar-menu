import { ApiError, type MenuImportResponse } from '@armenu/api-client';

import type { Workspace } from '../../auth/workspaces.ts';

/** The API refuses larger files; checking first spares uploading one for nothing. */
export const maxImportBytes = 1_048_576;

/** Downloads the menu spreadsheet under the name the API gives it. */
export async function downloadMenu(workspace: Workspace): Promise<void> {
  const result = await workspace.api.GET('/api/v1/manage/menu/export', { parseAs: 'blob' });
  if (!result.response.ok || result.data === undefined) {
    throw ApiError.fromResponse(result.response, result.error);
  }

  const name = fileNameOf(result.response.headers.get('Content-Disposition')) ?? `${workspace.slug}-menu.csv`;
  const url = URL.createObjectURL(result.data);
  const link = Object.assign(document.createElement('a'), { href: url, download: name });
  document.body.append(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

export async function importMenu(
  workspace: Workspace,
  csv: string,
  dryRun: boolean,
): Promise<MenuImportResponse> {
  const result = await workspace.api.POST('/api/v1/manage/menu/import', {
    params: { query: { dryRun } },
    body: csv,
    bodySerializer: (body: string) => body,
    headers: { 'Content-Type': 'text/csv; charset=utf-8' },
  });
  if (!result.response.ok || result.data === undefined) {
    throw ApiError.fromResponse(result.response, result.error);
  }

  return result.data;
}

/** The file name of a Content-Disposition header, preferring the UTF-8 form (`filename*=UTF-8''…`). */
export function fileNameOf(header: string | null): string | undefined {
  if (header === null) {
    return undefined;
  }

  const encoded = /filename\*=UTF-8''([^;]+)/i.exec(header)?.[1];
  if (encoded !== undefined) {
    return decodeURIComponent(encoded);
  }

  return /filename="?([^";]+)"?/i.exec(header)?.[1];
}
