import type { MenuImportResponse } from '@armenu/api-client';
import { useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { DropZone, FileTrigger, Text } from 'react-aria-components';

import { formErrorsFrom } from '../../api/form-errors.ts';
import { useWorkspace } from '../../app/workspace.ts';
import { useI18n } from '../../i18n/i18n-context.ts';
import { Button } from '../../ui/Button.tsx';
import { UploadIcon } from '../../ui/icons.tsx';
import { FormAlert, Modal } from '../../ui/Modal.tsx';
import { useNotify } from '../../ui/toaster-context.ts';
import { managedMenuQuery } from './menu-api.ts';
import { importMenu, maxImportBytes } from './menu-transfer.ts';

interface Chosen {
  readonly name: string;
  readonly csv: string;
  readonly preview: MenuImportResponse;
}

/** Choose a spreadsheet, see what it would change, then apply exactly that. */
export function MenuImportDialog({ onClose }: { onClose: () => void }) {
  const { messages, describeError } = useI18n();
  const workspace = useWorkspace();
  const queryClient = useQueryClient();
  const notify = useNotify();
  const [chosen, setChosen] = useState<Chosen>();
  const [problem, setProblem] = useState<string>();
  const [isPending, setIsPending] = useState(false);
  const text = messages.menuImport;

  const failed = (error: unknown) => {
    setProblem(formErrorsFrom(error, { describe: describeError, networkError: messages.networkError }).form);
  };

  const choose = async (file: File) => {
    setProblem(undefined);
    setChosen(undefined);
    if (file.size > maxImportBytes) {
      setProblem(describeError('menu_import.too_large'));
      return;
    }

    setIsPending(true);
    try {
      const csv = await file.text();
      setChosen({ name: file.name, csv, preview: await importMenu(workspace, csv, true) });
    } catch (error) {
      failed(error);
    } finally {
      setIsPending(false);
    }
  };

  const apply = async () => {
    if (chosen === undefined) {
      return;
    }

    setIsPending(true);
    try {
      const result = await importMenu(workspace, chosen.csv, false);
      if (result.applied) {
        await queryClient.invalidateQueries({ queryKey: managedMenuQuery(workspace).queryKey });
        notify(text.applied(result.created, result.updated));
        onClose();
        return;
      }

      // The menu changed since the preview in a way that makes the file invalid now.
      setChosen({ ...chosen, preview: result });
    } catch (error) {
      failed(error);
    } finally {
      setIsPending(false);
    }
  };

  const preview = chosen?.preview;
  const canApply = preview?.errors.length === 0 && preview.created + preview.updated > 0;

  return (
    <Modal
      title={text.title}
      size="lg"
      isOpen
      onOpenChange={(isOpen) => {
        if (!isOpen) {
          onClose();
        }
      }}
    >
      <div className="flex flex-col gap-4 text-sm">
        <ul className="list-disc space-y-1 ps-5 text-ink-muted">
          {text.rules.map((rule) => (
            <li key={rule}>{rule}</li>
          ))}
        </ul>

        <DropZone
          isDisabled={isPending}
          onDrop={(event) => {
            const dropped = event.items.find((entry) => entry.kind === 'file');
            if (dropped?.kind === 'file') {
              void dropped.getFile().then(choose);
            }
          }}
          className="flex flex-wrap items-center gap-2 rounded-lg border border-dashed border-line p-3 data-[drop-target]:border-accent data-[drop-target]:bg-accent-soft"
        >
          <FileTrigger
            acceptedFileTypes={['.csv', 'text/csv']}
            onSelect={(files) => {
              const file = files?.[0];
              if (file !== undefined) {
                void choose(file);
              }
            }}
          >
            <Button size="sm" isPending={isPending && chosen === undefined}>
              <UploadIcon />
              {messages.chooseFile}
            </Button>
          </FileTrigger>
          <Text slot="label" className="min-w-0 flex-1 truncate text-xs text-ink-muted">
            {chosen?.name ?? messages.dropFileHere}
          </Text>
        </DropZone>

        {problem !== undefined && <FormAlert message={problem} />}

        {preview !== undefined && (
          <section aria-live="polite" className="flex flex-col gap-3">
            {preview.errors.length > 0 ? (
              <>
                <p role="alert" className="font-medium text-danger">
                  {text.hasErrors(preview.errors.length)}
                </p>
                <div className="max-h-72 overflow-auto rounded-lg border border-line">
                  <table className="w-full text-start text-xs">
                    <caption className="sr-only">{text.errorsCaption}</caption>
                    <thead className="bg-sunken text-ink-muted">
                      <tr>
                        <th scope="col" className="px-3 py-2 text-start font-medium">
                          {text.line}
                        </th>
                        <th scope="col" className="px-3 py-2 text-start font-medium">
                          {text.column}
                        </th>
                        <th scope="col" className="px-3 py-2 text-start font-medium">
                          {text.problem}
                        </th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-line">
                      {preview.errors.map((error) => (
                        <tr key={`${error.line}-${error.column ?? ''}-${error.code}`}>
                          <td className="px-3 py-2 tabular-nums">{error.line}</td>
                          <td className="px-3 py-2 font-mono">{error.column ?? '—'}</td>
                          <td className="px-3 py-2">{describeError(error.code, error.message)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </>
            ) : (
              <>
                <p className="font-medium">
                  {text.summary(preview.created, preview.updated, preview.unchanged)}
                </p>
                {preview.changes.length > 0 && (
                  <ul className="max-h-72 divide-y divide-line overflow-auto rounded-lg border border-line">
                    {preview.changes.map((change) => (
                      <li key={change.line} className="flex flex-wrap items-baseline gap-x-3 px-3 py-2">
                        <span className="font-medium">{change.name}</span>
                        <span className="text-xs text-ink-muted">
                          {change.kind === 'created'
                            ? text.created
                            : change.fields
                                .map((field) => messages.history.fields[field] ?? field)
                                .join(', ')}
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </>
            )}
          </section>
        )}

        <div className="flex justify-end gap-2 border-t border-line pt-4">
          <Button onPress={onClose}>{messages.cancel}</Button>
          <Button
            variant="primary"
            isDisabled={!canApply}
            isPending={isPending && chosen !== undefined}
            onPress={() => void apply()}
          >
            {text.apply}
          </Button>
        </div>
      </div>
    </Modal>
  );
}
