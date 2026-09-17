import { languageName } from '@armenu/locale';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { Label, RadioButton, RadioField, RadioGroup } from 'react-aria-components';

import { formErrorsFrom } from '../../api/form-errors.ts';
import { ensureOk } from '../../api/result.ts';
import { meQuery, useCanEditMenu, useCurrentUser, useWorkspace } from '../../app/workspace.ts';
import { useI18n } from '../../i18n/i18n-context.ts';
import { Button } from '../../ui/Button.tsx';
import { SelectField } from '../../ui/fields.tsx';
import { Card } from '../../ui/layout.tsx';
import { FormAlert } from '../../ui/Modal.tsx';
import { useNotify } from '../../ui/toaster-context.ts';
import { managedMenuQuery } from '../menu/menu-api.ts';

const maxLanguages = 10;

// Languages of tourists in Turkey first, then other widely read ones.
const languageChoices = [
  'tr',
  'en',
  'de',
  'ru',
  'ar',
  'fa',
  'fr',
  'es',
  'it',
  'nl',
  'pl',
  'uk',
  'az',
  'el',
  'he',
  'pt',
  'sv',
  'zh',
  'ja',
  'ko',
];

/** The offered languages and the default one; owners and managers edit them, staff read them. */
export function LanguagesCard() {
  const { messages, describeError } = useI18n();
  const workspace = useWorkspace();
  const { tenant } = useCurrentUser();
  const canEdit = useCanEditMenu();
  const queryClient = useQueryClient();
  const notify = useNotify();
  const [cultures, setCultures] = useState<string[]>([...tenant.supportedCultures]);
  const [defaultCulture, setDefaultCulture] = useState(tenant.defaultCulture);

  const save = useMutation({
    mutationFn: async () => {
      ensureOk(
        await workspace.api.PUT('/api/v1/manage/settings/languages', {
          body: { defaultCulture, supportedCultures: cultures },
        }),
      );
    },
    onSuccess: async () => {
      notify(messages.languagesSaved);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: meQuery(workspace).queryKey }),
        queryClient.invalidateQueries({ queryKey: managedMenuQuery(workspace).queryKey }),
      ]);
    },
  });

  const isDirty =
    defaultCulture !== tenant.defaultCulture ||
    cultures.length !== tenant.supportedCultures.length ||
    cultures.some((culture, index) => culture !== tenant.supportedCultures[index]);
  const error = save.isError
    ? formErrorsFrom(save.error, { describe: describeError, networkError: messages.networkError })
    : undefined;

  return (
    <Card className="p-5" label={messages.navLanguages}>
      <h2 className="text-base font-semibold">{messages.navLanguages}</h2>
      <p className="mt-1 mb-4 text-sm text-ink-muted">{messages.languagesIntro}</p>
      <RadioGroup
        value={defaultCulture}
        onChange={setDefaultCulture}
        isDisabled={!canEdit}
        className="flex flex-col gap-2"
      >
        <Label className="mb-1 text-sm font-medium">{messages.defaultLanguage}</Label>
        {cultures.map((culture) => {
          const name = languageName(culture);
          return (
            <div key={culture} className="flex items-center gap-3 rounded-lg border border-line px-3 py-2">
              <RadioField value={culture} aria-label={messages.makeDefault(name)} className="flex flex-1">
                <RadioButton className="group flex flex-1 cursor-default items-center gap-3 text-sm outline-none">
                  <span
                    aria-hidden="true"
                    className="size-4 rounded-full border-2 border-line group-data-[focus-visible]:outline-2 group-data-[focus-visible]:outline-offset-2 group-data-[focus-visible]:outline-accent group-data-[selected]:border-[5px] group-data-[selected]:border-accent"
                  />
                  <span lang={culture} className="font-medium">
                    {name}
                  </span>
                  <span className="text-xs text-ink-muted">{culture}</span>
                  {culture === defaultCulture && (
                    <span className="rounded bg-accent-soft px-1.5 text-xs font-medium text-accent">
                      {messages.defaultLanguageBadge}
                    </span>
                  )}
                </RadioButton>
              </RadioField>
              {canEdit && (
                <Button
                  size="sm"
                  variant="ghost"
                  aria-label={messages.removeLanguage(name)}
                  isDisabled={culture === defaultCulture}
                  onPress={() => {
                    setCultures((current) => current.filter((candidate) => candidate !== culture));
                  }}
                >
                  {messages.removeFile}
                </Button>
              )}
            </div>
          );
        })}
      </RadioGroup>

      {canEdit && (
        <>
          <SelectField
            className="mt-5 max-w-xs"
            label={messages.addLanguage}
            options={languageChoices
              .filter((culture) => !cultures.includes(culture))
              .map((culture) => ({ id: culture, label: languageName(culture) }))}
            value={null}
            isDisabled={cultures.length >= maxLanguages}
            onChange={(key) => {
              if (key !== null) {
                setCultures((current) => [...current, String(key)]);
              }
            }}
          />
          <p className="mt-4 text-xs text-ink-muted">{messages.translationsKept}</p>
          {error?.form !== undefined && <FormAlert message={error.form} />}
          {error !== undefined && error.form === undefined && (
            <FormAlert message={Object.values(error.fields).join(' ')} />
          )}
          <div className="mt-5 flex justify-end border-t border-line pt-4">
            <Button
              variant="primary"
              isDisabled={!isDirty}
              isPending={save.isPending}
              onPress={() => {
                save.mutate();
              }}
            >
              {messages.save}
            </Button>
          </div>
        </>
      )}
    </Card>
  );
}
