import { ApiError } from '@armenu/api-client';

export interface FormErrors {
  /** Messages keyed by form field name (`price`, `name.tr`), the shape React Aria forms display. */
  readonly fields: Readonly<Record<string, string>>;
  /** A problem that belongs to no single field. */
  readonly form: string | undefined;
}

export const noFormErrors: FormErrors = { fields: {}, form: undefined };

interface ErrorText {
  readonly describe: (code: string | undefined, fallback?: string) => string;
  readonly networkError: string;
}

/**
 * Turns a failed request into messages next to the fields they concern. Validation problems carry stable codes per
 * field (`errorCodes`), which are translated; the API's English messages are only a fallback.
 */
export function formErrorsFrom(error: unknown, text: ErrorText): FormErrors {
  if (!(error instanceof ApiError)) {
    // fetch rejects with a TypeError when the network or CORS fails.
    return { fields: {}, form: error instanceof TypeError ? text.networkError : text.describe(undefined) };
  }

  const codes = readStringArrays(error.problem, 'errorCodes');
  const messages = readStringArrays(error.problem, 'errors');
  const fields: Record<string, string> = {};

  for (const [path, fieldCodes] of Object.entries(codes)) {
    fields[toFieldName(path)] = text.describe(fieldCodes[0], messages[path]?.[0]);
  }

  return Object.keys(fields).length > 0
    ? { fields, form: undefined }
    : { fields, form: text.describe(error.code, error.problem?.detail ?? undefined) };
}

/** Validation paths use C#-style indexers (`name[tr]`); form fields use dots (`name.tr`). */
export function toFieldName(path: string): string {
  return path.replace(/\[([^\]]+)\]/g, '.$1');
}

function readStringArrays(problem: unknown, property: string): Record<string, readonly string[]> {
  const value =
    typeof problem === 'object' && problem !== null
      ? (problem as Record<string, unknown>)[property]
      : undefined;
  if (typeof value !== 'object' || value === null) {
    return {};
  }

  return Object.fromEntries(
    Object.entries(value).filter(
      (entry): entry is [string, string[]] =>
        Array.isArray(entry[1]) && entry[1].every((item) => typeof item === 'string'),
    ),
  );
}
