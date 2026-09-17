import {
  ApiError,
  type ApiClient,
  type ArModelProcessingResponse,
  type AssetKind,
  type AssetUpload,
  type PublishedAsset,
} from '@armenu/api-client';

import { unwrap } from '../../api/result.ts';

interface AssetRule {
  readonly extensions: readonly string[];
  readonly maxBytes: number;
}

const megabyte = 1024 * 1024;

const imageContentTypes: Readonly<Record<string, string>> = {
  '.webp': 'image/webp',
  '.avif': 'image/avif',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
};

/** The same limits the API enforces, checked before a byte leaves the browser. */
export const assetRules: Readonly<Record<AssetKind, AssetRule>> = {
  // Raw exports and scans: processing makes them small.
  model: { extensions: ['.glb'], maxBytes: 64 * megabyte },
  appleModel: { extensions: ['.usdz'], maxBytes: 25 * megabyte },
  poster: { extensions: Object.keys(imageContentTypes), maxBytes: 2 * megabyte },
  // Every guest downloads the logo before anything else on the menu.
  logo: { extensions: Object.keys(imageContentTypes), maxBytes: 512 * 1024 },
};

interface FileInfo {
  readonly name: string;
  readonly size: number;
}

/** An API error code for a file the API would refuse, or undefined for an acceptable file. */
export function checkFile(kind: AssetKind, file: FileInfo): string | undefined {
  const rule = assetRules[kind];
  if (!rule.extensions.includes(extensionOf(file.name))) {
    return 'asset.content_type_not_allowed';
  }

  if (file.size === 0) {
    return 'asset.size_invalid';
  }

  return file.size > rule.maxBytes ? 'asset.too_large' : undefined;
}

/**
 * The type the file is uploaded with, from its extension: browsers report no type at all for .glb and .usdz files on
 * most systems. The API inspects the bytes anyway before publishing.
 */
export function contentTypeFor(kind: AssetKind, fileName: string): string {
  switch (kind) {
    case 'model':
      return 'model/gltf-binary';
    case 'appleModel':
      return 'model/vnd.usdz+zip';
    case 'poster':
    case 'logo':
      return imageContentTypes[extensionOf(fileName)] ?? 'application/octet-stream';
  }
}

export type PutFile = (
  upload: AssetUpload,
  file: Blob,
  onProgress: (share: number) => void,
  signal?: AbortSignal,
) => Promise<void>;

interface UploadOptions {
  readonly onProgress?: (share: number) => void;
  readonly signal?: AbortSignal;
  readonly put?: PutFile;
}

/** Uploads a file straight to storage: a presigned upload for exactly this file, then the upload itself. */
export async function sendToStorage(
  api: Pick<ApiClient, 'POST'>,
  kind: AssetKind,
  file: File,
  { onProgress = () => undefined, signal, put = putWithProgress }: UploadOptions = {},
): Promise<AssetUpload> {
  const upload = unwrap(
    await api.POST('/api/v1/manage/assets/uploads', {
      body: { kind, contentType: contentTypeFor(kind, file.name), size: file.size },
      signal: signal ?? null,
    }),
  );

  await put(upload, file, onProgress, signal);
  return upload;
}

/** Uploads a poster or an iOS model and has the API inspect and publish it. Returns the path to attach to an item. */
export async function uploadAsset(
  api: Pick<ApiClient, 'POST'>,
  kind: Exclude<AssetKind, 'model'>,
  file: File,
  options: UploadOptions = {},
): Promise<PublishedAsset> {
  const upload = await sendToStorage(api, kind, file, options);

  return unwrap(
    await api.POST('/api/v1/manage/assets/uploads/{uploadId}/publish', {
      params: { path: { uploadId: upload.uploadId } },
      body: { kind },
      signal: options.signal ?? null,
    }),
  );
}

/**
 * Uploads a GLB and hands it to processing, which creates every file the item's model needs and publishes them when
 * ready. The item keeps its current model until then.
 */
export async function uploadModelForProcessing(
  api: Pick<ApiClient, 'POST'>,
  itemId: string,
  file: File,
  options: UploadOptions = {},
): Promise<ArModelProcessingResponse> {
  const upload = await sendToStorage(api, 'model', file, options);

  return unwrap(
    await api.POST('/api/v1/manage/menu/items/{itemId}/ar-model/processing', {
      params: { path: { itemId } },
      body: { uploadId: upload.uploadId },
      signal: options.signal ?? null,
    }),
  );
}

/** fetch cannot report upload progress; XMLHttpRequest can. */
export const putWithProgress: PutFile = (upload, file, onProgress, signal) =>
  new Promise((resolve, reject) => {
    const request = new XMLHttpRequest();
    request.open(upload.method, upload.url);
    for (const [name, value] of Object.entries(upload.headers)) {
      request.setRequestHeader(name, value);
    }

    request.upload.onprogress = (event) => {
      if (event.lengthComputable) {
        onProgress(event.loaded / event.total);
      }
    };
    request.onload = () => {
      if (request.status >= 200 && request.status < 300) {
        resolve();
      } else {
        // Storage refuses expired or altered uploads (403): the remedy is choosing the file again.
        reject(new ApiError(request.status, { status: request.status, code: 'asset.upload_not_found' }));
      }
    };
    request.onerror = () => {
      reject(new TypeError('The upload could not reach storage.'));
    };
    request.onabort = () => {
      reject(new DOMException('The upload was cancelled.', 'AbortError'));
    };
    signal?.addEventListener('abort', () => {
      request.abort();
    });

    request.send(file);
  });

function extensionOf(fileName: string): string {
  const dot = fileName.lastIndexOf('.');
  return dot === -1 ? '' : fileName.slice(dot).toLowerCase();
}
