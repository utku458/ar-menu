import type { ArModelProcessingResponse, ManagedMenuItemResponse } from '@armenu/api-client';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { lazy, type ReactNode, Suspense, useEffect, useRef, useState } from 'react';
import { DropZone, FileTrigger, ProgressBar, Text } from 'react-aria-components';

import { formErrorsFrom } from '../../api/form-errors.ts';
import { useWorkspace } from '../../app/workspace.ts';
import { useI18n } from '../../i18n/i18n-context.ts';
import { Button } from '../../ui/Button.tsx';
import { CubeIcon, UploadIcon } from '../../ui/icons.tsx';
import { useNotify } from '../../ui/toaster-context.ts';
import { managedMenuQuery, useMenuMutations } from '../menu/menu-api.ts';
import { formatBytes, formatDimensions, isActive, processingQuery, reductionPercent } from './processing.ts';
import { assetRules, checkFile, uploadAsset, uploadModelForProcessing } from './upload-asset.ts';

const ArViewer = lazy(async () => ({ default: (await import('@armenu/ar-viewer')).ArViewer }));

type Slot = 'model' | 'appleModel' | 'poster';

/**
 * The 3D model of a dish. The business uploads one GLB; processing publishes the optimized model, the Scene Viewer
 * model, the USDZ and the poster, and the preview switches to them once they are live. The generated USDZ and poster
 * can be replaced with the business's own.
 */
export function ArModelPanel({ item, itemName }: { item: ManagedMenuItemResponse; itemName: string }) {
  const { messages, describeError } = useI18n();
  const workspace = useWorkspace();
  const queryClient = useQueryClient();
  const notify = useNotify();
  const { attachArModel, detachArModel } = useMenuMutations();
  const processing = useQuery(processingQuery(workspace, item.id));
  const [progress, setProgress] = useState<Partial<Record<Slot, number>>>({});
  const [errors, setErrors] = useState<Partial<Record<Slot, string>>>({});
  const uploads = useRef(new Set<AbortController>());

  // Closing the dialog cancels uploads in flight instead of finishing them for nobody.
  useEffect(() => {
    const active = uploads.current;
    return () => {
      for (const controller of active) {
        controller.abort();
      }
    };
  }, []);

  // Processing finishes in the background: when a watched processing publishes, the menu (and this preview) follow.
  const watchedStatus = useRef(processing.data?.status);
  const status = processing.data?.status;
  useEffect(() => {
    if (isActive(watchedStatus.current) && status === 'succeeded') {
      notify(messages.modelPublished);
      void queryClient.invalidateQueries({ queryKey: managedMenuQuery(workspace).queryKey });
    }
    watchedStatus.current = status;
  }, [status, messages.modelPublished, notify, queryClient, workspace]);

  const describe = (error: unknown) =>
    formErrorsFrom(error, { describe: describeError, networkError: messages.networkError }).form ??
    messages.unexpectedError;

  async function upload(slot: Slot, file: File) {
    const invalid = checkFile(slot, file);
    setErrors((current) => ({
      ...current,
      [slot]: invalid === undefined ? undefined : describeError(invalid),
    }));
    if (invalid !== undefined) {
      return;
    }

    const controller = new AbortController();
    uploads.current.add(controller);
    setProgress((current) => ({ ...current, [slot]: 0 }));
    const options = {
      signal: controller.signal,
      onProgress: (share: number) => {
        setProgress((current) => ({ ...current, [slot]: share }));
      },
    };

    try {
      if (slot === 'model') {
        const started = await uploadModelForProcessing(workspace.api, item.id, file, options);
        watchedStatus.current = started.status;
        queryClient.setQueryData(processingQuery(workspace, item.id).queryKey, started);
      } else {
        await replaceGeneratedFile(slot, (await uploadAsset(workspace.api, slot, file, options)).path);
      }
    } catch (error) {
      if (!controller.signal.aborted) {
        setErrors((current) => ({ ...current, [slot]: describe(error) }));
      }
    } finally {
      uploads.current.delete(controller);
      setProgress((current) => ({ ...current, [slot]: undefined }));
    }
  }

  async function replaceGeneratedFile(slot: 'appleModel' | 'poster', path: string) {
    const model = item.arModel;
    if (model === null) {
      return;
    }

    await attachArModel.mutateAsync({
      id: item.id,
      body: {
        glbPath: model.glbPath,
        sceneViewerGlbPath: model.sceneViewerGlbPath,
        usdzPath: slot === 'appleModel' ? path : model.usdzPath,
        posterPath: slot === 'poster' ? path : model.posterPath,
      },
    });
    notify(messages.fileReplaced);
  }

  async function remove() {
    try {
      await detachArModel.mutateAsync(item.id);
      notify(messages.modelRemoved);
    } catch (error) {
      notify(describe(error), 'error');
    }
  }

  const model = item.arModel;
  const isProcessing = isActive(status);

  return (
    <div className="grid gap-6 md:grid-cols-[minmax(0,1fr)_minmax(0,1.1fr)]">
      <figure className="m-0">
        <div className="relative aspect-square overflow-hidden rounded-xl bg-sunken">
          {model === null ? (
            <div className="flex size-full flex-col items-center justify-center gap-2 text-ink-muted">
              <CubeIcon className="size-10" />
              <span className="text-sm">{messages.noModelYet}</span>
            </div>
          ) : (
            <Suspense fallback={<p className="p-4 text-sm text-ink-muted">{messages.loading}</p>}>
              <ArViewer
                key={model.glbUrl}
                src={model.glbUrl}
                sceneViewerSrc={model.sceneViewerGlbUrl ?? undefined}
                iosSrc={model.usdzUrl ?? undefined}
                poster={model.posterUrl ?? undefined}
                alt={messages.previewAlt(itemName)}
                className="block size-full"
              />
            </Suspense>
          )}
        </div>
        <figcaption className="mt-2 text-xs text-ink-muted">{messages.preview}</figcaption>
      </figure>

      <div className="flex flex-col gap-4">
        <FileSlot
          slot="model"
          label={messages.modelFile}
          description={messages.modelFileDescription}
          hasFile={model !== null}
          progress={progress.model}
          error={errors.model}
          isDisabled={isProcessing}
          onFile={(file) => void upload('model', file)}
        >
          {processing.data && <ProcessingStatus processing={processing.data} />}
        </FileSlot>

        {model !== null && (
          <details className="rounded-xl border border-line p-3">
            <summary className="cursor-default text-sm font-medium">{messages.replaceGeneratedFiles}</summary>
            <div className="mt-3 flex flex-col gap-3">
              <FileSlot
                slot="appleModel"
                label={messages.appleModelFile}
                description={messages.appleModelFileDescription}
                hasFile={model.usdzPath !== null}
                progress={progress.appleModel}
                error={errors.appleModel}
                isDisabled={isProcessing}
                onFile={(file) => void upload('appleModel', file)}
              />
              <FileSlot
                slot="poster"
                label={messages.posterFile}
                description={messages.posterFileDescription}
                hasFile={model.posterPath !== null}
                progress={progress.poster}
                error={errors.poster}
                isDisabled={isProcessing}
                onFile={(file) => void upload('poster', file)}
              />
            </div>
          </details>
        )}

        {model !== null && (
          <div className="flex justify-end border-t border-line pt-4">
            <Button variant="ghost" onPress={() => void remove()} isPending={detachArModel.isPending}>
              {messages.removeModel}
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}

function ProcessingStatus({ processing }: { processing: ArModelProcessingResponse }) {
  const { messages, describeError, language } = useI18n();
  const locale = language === 'tr' ? 'tr-TR' : 'en-US';

  if (isActive(processing.status)) {
    const label = processing.status === 'queued' ? messages.processingQueued : messages.processingRunning;
    return (
      <div className="mt-3 rounded-lg bg-accent-soft p-3">
        <ProgressBar isIndeterminate aria-label={label}>
          <p className="text-sm font-medium">{label}</p>
          <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-surface">
            <div className="h-full w-1/3 animate-pulse rounded-full bg-accent" />
          </div>
        </ProgressBar>
        <p className="mt-2 text-xs text-ink-muted">{messages.processingKeepsCurrentModel}</p>
      </div>
    );
  }

  if (processing.status === 'failed') {
    return (
      <div role="alert" className="mt-3 rounded-lg bg-danger-soft p-3 text-sm">
        <p className="font-medium text-danger">{messages.processingFailed}</p>
        <p className="mt-1">{describeError(processing.failureCode ?? undefined)}</p>
      </div>
    );
  }

  const report = processing.report;
  if (processing.status !== 'succeeded' || report === null) {
    return null;
  }

  const count = new Intl.NumberFormat(locale);
  return (
    <section aria-label={messages.latestProcessing} className="mt-3 rounded-lg bg-sunken p-3 text-sm">
      <p className="font-medium">{messages.latestProcessing}</p>
      <ul className="mt-1 flex flex-col gap-0.5 text-ink-muted">
        <li>
          {messages.reportDownload(
            formatBytes(report.files.source, locale),
            formatBytes(report.files.model, locale),
            reductionPercent(report.files.source, report.files.model),
          )}
        </li>
        <li>
          {messages.reportTriangles(
            count.format(report.source.triangles),
            count.format(report.optimized.triangles),
          )}
        </li>
        <li>{messages.reportSizeOnTable(formatDimensions(report.dimensions, locale))}</li>
      </ul>
      {report.warnings.length > 0 && (
        <ul className="mt-2 flex list-disc flex-col gap-0.5 ps-5">
          {report.warnings.map((warning) => (
            <li key={warning}>{messages.processingWarning(warning)}</li>
          ))}
        </ul>
      )}
    </section>
  );
}

interface FileSlotProps {
  readonly slot: Slot;
  readonly label: string;
  readonly description: string;
  readonly hasFile: boolean;
  readonly progress: number | undefined;
  readonly error: string | undefined;
  readonly isDisabled: boolean;
  readonly onFile: (file: File) => void;
  readonly children?: ReactNode;
}

function FileSlot({
  slot,
  label,
  description,
  hasFile,
  progress,
  error,
  isDisabled,
  onFile,
  children,
}: FileSlotProps) {
  const { messages } = useI18n();
  const isBusy = isDisabled || progress !== undefined;

  return (
    <section aria-label={label} className="rounded-xl border border-line p-3">
      <p className="text-sm font-medium">{label}</p>
      <p className="mt-0.5 text-xs text-ink-muted">{description}</p>

      <DropZone
        isDisabled={isBusy}
        onDrop={(event) => {
          const dropped = event.items.find((entry) => entry.kind === 'file');
          if (dropped?.kind === 'file') {
            void dropped.getFile().then(onFile);
          }
        }}
        className="mt-3 flex flex-wrap items-center gap-2 rounded-lg border border-dashed border-line p-2 data-[drop-target]:border-accent data-[drop-target]:bg-accent-soft"
      >
        <FileTrigger
          acceptedFileTypes={[...assetRules[slot].extensions]}
          onSelect={(selected) => {
            const chosen = selected?.[0];
            if (chosen !== undefined) {
              onFile(chosen);
            }
          }}
        >
          <Button size="sm" isDisabled={isBusy}>
            <UploadIcon />
            {hasFile ? messages.replaceFile : messages.chooseFile}
          </Button>
        </FileTrigger>
        <Text slot="label" className="min-w-0 flex-1 truncate text-xs text-ink-muted">
          {messages.dropFileHere}
        </Text>
      </DropZone>

      {progress !== undefined && (
        <ProgressBar value={progress * 100} aria-label={`${messages.uploading}: ${label}`} className="mt-2">
          {({ percentage }) => (
            <div className="h-1.5 overflow-hidden rounded-full bg-sunken">
              <div className="h-full bg-accent transition-[width]" style={{ width: `${percentage ?? 0}%` }} />
            </div>
          )}
        </ProgressBar>
      )}
      {error !== undefined && (
        <p role="alert" className="mt-2 text-sm text-danger">
          {error}
        </p>
      )}
      {children}
    </section>
  );
}
