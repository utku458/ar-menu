import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useRef, useState } from 'react';
import { DropZone, FileTrigger, ProgressBar, Text } from 'react-aria-components';

import { formErrorsFrom } from '../../api/form-errors.ts';
import { ensureOk } from '../../api/result.ts';
import { meQuery, useCanEditMenu, useCurrentUser, useWorkspace } from '../../app/workspace.ts';
import { useI18n } from '../../i18n/i18n-context.ts';
import { Button } from '../../ui/Button.tsx';
import { TextField } from '../../ui/fields.tsx';
import { UploadIcon } from '../../ui/icons.tsx';
import { Card } from '../../ui/layout.tsx';
import { FormAlert } from '../../ui/Modal.tsx';
import { useNotify } from '../../ui/toaster-context.ts';
import { assetRules, checkFile, uploadAsset } from '../ar/upload-asset.ts';
import { managedMenuQuery } from '../menu/menu-api.ts';

/** Shown in the picker when a business has no colour yet, so the first drag starts somewhere sensible. */
const startingColor = '#a34a06';

interface Logo {
  readonly path: string;
  readonly url: string;
}

/**
 * The name, logo and colour guests see at the top of the menu. The preview only fills a shape with the colour, never
 * writes on it: the API decides what text stays legible on a business's colour, and it is the menu that shows it.
 */
export function BrandingCard() {
  const { messages, describeError } = useI18n();
  const workspace = useWorkspace();
  const { tenant } = useCurrentUser();
  const canEdit = useCanEditMenu();
  const queryClient = useQueryClient();
  const notify = useNotify();

  const [name, setName] = useState(tenant.name);
  const [logo, setLogo] = useState<Logo | null>(
    tenant.logoPath === null || tenant.logoUrl === null
      ? null
      : { path: tenant.logoPath, url: tenant.logoUrl },
  );
  const [color, setColor] = useState<string | null>(tenant.accentColor);
  const [progress, setProgress] = useState<number>();
  const [uploadError, setUploadError] = useState<string>();
  const upload = useRef<AbortController>(null);

  // Leaving the page cancels an upload in flight instead of finishing it for nobody.
  useEffect(
    () => () => {
      upload.current?.abort();
    },
    [],
  );

  const save = useMutation({
    mutationFn: async () => {
      ensureOk(
        await workspace.api.PUT('/api/v1/manage/settings/branding', {
          body: { name, logoPath: logo?.path ?? null, accentColor: color },
        }),
      );
    },
    onSuccess: async () => {
      notify(messages.brandingSaved);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: meQuery(workspace).queryKey }),
        queryClient.invalidateQueries({ queryKey: managedMenuQuery(workspace).queryKey }),
      ]);
    },
  });

  async function chooseLogo(file: File) {
    const invalid = checkFile('logo', file);
    setUploadError(invalid === undefined ? undefined : describeError(invalid));
    if (invalid !== undefined) {
      return;
    }

    const controller = new AbortController();
    upload.current = controller;
    setProgress(0);

    try {
      const published = await uploadAsset(workspace.api, 'logo', file, {
        signal: controller.signal,
        onProgress: setProgress,
      });
      setLogo({ path: published.path, url: published.url });
    } catch (error) {
      if (!controller.signal.aborted) {
        setUploadError(
          formErrorsFrom(error, { describe: describeError, networkError: messages.networkError }).form ??
            messages.unexpectedError,
        );
      }
    } finally {
      upload.current = null;
      setProgress(undefined);
    }
  }

  const isDirty =
    name !== tenant.name || (logo?.path ?? null) !== tenant.logoPath || color !== tenant.accentColor;
  const error = save.isError
    ? formErrorsFrom(save.error, { describe: describeError, networkError: messages.networkError })
    : undefined;

  return (
    <Card className="p-5" label={messages.branding}>
      <h2 className="text-base font-semibold">{messages.branding}</h2>
      <p className="mt-1 mb-4 text-sm text-ink-muted">{messages.brandingIntro}</p>

      <TextField label={messages.businessName} value={name} onChange={setName} isDisabled={!canEdit} />

      <section aria-label={messages.logo} className="mt-5">
        <p className="text-sm font-medium">{messages.logo}</p>
        <p className="mt-0.5 text-xs text-ink-muted">{messages.logoIntro}</p>
        <div className="mt-3 flex items-center gap-4">
          <div className="grid size-16 shrink-0 place-items-center overflow-hidden rounded-xl border border-line bg-sunken">
            {logo === null ? (
              <span aria-hidden="true" className="text-xs text-ink-muted">
                —
              </span>
            ) : (
              <img src={logo.url} alt={messages.logo} className="size-full object-contain" />
            )}
          </div>
          {canEdit && (
            <DropZone
              isDisabled={progress !== undefined}
              onDrop={(event) => {
                const dropped = event.items.find((entry) => entry.kind === 'file');
                if (dropped?.kind === 'file') {
                  void dropped.getFile().then(chooseLogo);
                }
              }}
              className="flex flex-1 flex-wrap items-center gap-2 rounded-lg border border-dashed border-line p-2 data-[drop-target]:border-accent data-[drop-target]:bg-accent-soft"
            >
              <FileTrigger
                acceptedFileTypes={[...assetRules.logo.extensions]}
                onSelect={(selected) => {
                  const chosen = selected?.[0];
                  if (chosen !== undefined) {
                    void chooseLogo(chosen);
                  }
                }}
              >
                <Button size="sm" isDisabled={progress !== undefined}>
                  <UploadIcon />
                  {logo === null ? messages.chooseFile : messages.replaceFile}
                </Button>
              </FileTrigger>
              {logo !== null && (
                <Button
                  size="sm"
                  variant="ghost"
                  isDisabled={progress !== undefined}
                  onPress={() => {
                    setLogo(null);
                  }}
                >
                  {messages.removeLogo}
                </Button>
              )}
              <Text slot="label" className="min-w-0 flex-1 truncate text-xs text-ink-muted">
                {messages.dropFileHere}
              </Text>
            </DropZone>
          )}
        </div>
        {progress !== undefined && (
          <ProgressBar
            value={progress * 100}
            aria-label={`${messages.uploading}: ${messages.logo}`}
            className="mt-2"
          >
            {({ percentage }) => (
              <div className="h-1.5 overflow-hidden rounded-full bg-sunken">
                <div
                  className="h-full bg-accent transition-[width]"
                  style={{ width: `${percentage ?? 0}%` }}
                />
              </div>
            )}
          </ProgressBar>
        )}
        {uploadError !== undefined && (
          <p role="alert" className="mt-2 text-sm text-danger">
            {uploadError}
          </p>
        )}
      </section>

      <section aria-label={messages.brandColor} className="mt-5">
        <p className="text-sm font-medium">{messages.brandColor}</p>
        <p className="mt-0.5 text-xs text-ink-muted">{messages.brandColorIntro}</p>
        <div className="mt-3 flex flex-wrap items-center gap-3">
          <input
            type="color"
            aria-label={messages.brandColor}
            value={color ?? startingColor}
            disabled={!canEdit}
            onChange={(event) => {
              setColor(event.target.value);
            }}
            className="size-10 cursor-pointer rounded-lg border border-line bg-surface p-1"
          />
          <output className="font-mono text-sm text-ink-muted">{color ?? messages.optional}</output>
          {canEdit && color !== null && (
            <Button
              size="sm"
              variant="ghost"
              onPress={() => {
                setColor(null);
              }}
            >
              {messages.useDefaultColor}
            </Button>
          )}
        </div>
        {/*
          What the menu's band looks like. Decorative: the chosen colour is already written out next to the picker,
          so announcing the band as an image would only repeat it.
        */}
        <div
          aria-hidden="true"
          className="mt-3 h-2 rounded-full bg-accent"
          style={{ backgroundColor: color ?? undefined }}
        />
      </section>

      {canEdit && (
        <>
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
