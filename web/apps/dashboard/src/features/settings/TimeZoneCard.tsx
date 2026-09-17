import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useMemo, useState } from 'react';

import { formErrorsFrom } from '../../api/form-errors.ts';
import { ensureOk } from '../../api/result.ts';
import { meQuery, useCanEditMenu, useCurrentUser, useWorkspace } from '../../app/workspace.ts';
import { useI18n } from '../../i18n/i18n-context.ts';
import { Button } from '../../ui/Button.tsx';
import { ComboBoxField } from '../../ui/fields.tsx';
import { Card } from '../../ui/layout.tsx';
import { FormAlert } from '../../ui/Modal.tsx';
import { useNotify } from '../../ui/toaster-context.ts';
import { deviceTimeZone, timeZoneOptions } from './time-zones.ts';

/** Where the business's day begins, for its statistics. */
export function TimeZoneCard() {
  const { messages, language, describeError } = useI18n();
  const workspace = useWorkspace();
  const { tenant } = useCurrentUser();
  const canEdit = useCanEditMenu();
  const queryClient = useQueryClient();
  const notify = useNotify();
  // Null while the person clears the field to type another name.
  const [timeZone, setTimeZone] = useState<string | null>(tenant.timeZone);
  const options = useMemo(() => timeZoneOptions(tenant.timeZone, language), [tenant.timeZone, language]);
  const device = deviceTimeZone();

  const save = useMutation({
    mutationFn: async (chosen: string) => {
      ensureOk(await workspace.api.PUT('/api/v1/manage/settings/time-zone', { body: { timeZone: chosen } }));
    },
    onSuccess: async () => {
      notify(messages.timeZoneSaved);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: meQuery(workspace).queryKey }),
        queryClient.invalidateQueries({ queryKey: ['statistics', workspace.slug] }),
      ]);
    },
  });
  const error = save.isError
    ? formErrorsFrom(save.error, { describe: describeError, networkError: messages.networkError })
    : undefined;

  return (
    <Card className="p-5" label={messages.timeZone}>
      <h2 className="text-base font-semibold">{messages.timeZone}</h2>
      <p className="mt-1 mb-4 text-sm text-ink-muted">{messages.timeZoneIntro}</p>

      <ComboBoxField
        className="max-w-md"
        label={messages.timeZone}
        options={options}
        value={timeZone}
        isDisabled={!canEdit}
        onChange={(key) => {
          setTimeZone(key === null ? null : String(key));
        }}
      />

      {canEdit && device !== timeZone && options.some((option) => option.id === device) && (
        <p className="mt-2 flex flex-wrap items-center gap-x-2 text-xs text-ink-muted">
          {messages.deviceTimeZone(device.replaceAll('_', ' '))}
          <Button
            size="sm"
            variant="ghost"
            onPress={() => {
              setTimeZone(device);
            }}
          >
            {messages.useDeviceTimeZone}
          </Button>
        </p>
      )}

      {error !== undefined && <FormAlert message={error.form ?? Object.values(error.fields).join(' ')} />}

      {canEdit && (
        <div className="mt-5 flex justify-end border-t border-line pt-4">
          <Button
            variant="primary"
            isDisabled={timeZone === null || timeZone === tenant.timeZone}
            isPending={save.isPending}
            onPress={() => {
              if (timeZone !== null) {
                save.mutate(timeZone);
              }
            }}
          >
            {messages.save}
          </Button>
        </div>
      )}
    </Card>
  );
}
