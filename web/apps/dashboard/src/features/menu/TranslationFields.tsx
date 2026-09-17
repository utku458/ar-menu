import { languageName, textDirection } from '@armenu/locale';

import { useI18n } from '../../i18n/i18n-context.ts';
import { TextField } from '../../ui/fields.tsx';

interface TranslationFieldsProps {
  /** Form field prefix; inputs are named `{field}.{culture}`. */
  readonly field: string;
  readonly label: string;
  readonly cultures: readonly string[];
  readonly defaultCulture: string;
  readonly values: Readonly<Record<string, string>> | null | undefined;
  readonly isRequired?: boolean;
  readonly multiline?: boolean;
  readonly maxLength?: number;
}

/** One input per offered language, written and spell-checked in that language's direction. */
export function TranslationFields({
  field,
  label,
  cultures,
  defaultCulture,
  values,
  isRequired = false,
  multiline = false,
  maxLength,
}: TranslationFieldsProps) {
  const { messages } = useI18n();

  return (
    <div className="flex flex-col gap-3">
      {cultures.map((culture) => (
        <TextField
          key={culture}
          name={`${field}.${culture}`}
          label={`${label} · ${languageName(culture)}${culture === defaultCulture ? ` (${messages.defaultLanguageBadge})` : ''}`}
          defaultValue={values?.[culture] ?? ''}
          isRequired={isRequired && culture === defaultCulture}
          multiline={multiline}
          {...(maxLength === undefined ? {} : { maxLength })}
          lang={culture}
          dir={textDirection(culture)}
        />
      ))}
    </div>
  );
}
