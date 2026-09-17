import type { Document, Texture } from '@gltf-transform/core';
import { EXTTextureAVIF, EXTTextureWebP } from '@gltf-transform/extensions';
import sharp from 'sharp';

import { ModelRejectedError } from './rejection.ts';

export type TextureFormat = 'jpeg' | 'png' | 'webp';

export interface TextureEncoding {
  readonly maxSize: number;
  /** The format a texture should end up in, knowing whether it uses transparency. */
  readonly format: (isOpaque: boolean) => TextureFormat;
}

const mimeTypes: Readonly<Record<TextureFormat, string>> = {
  jpeg: 'image/jpeg',
  png: 'image/png',
  webp: 'image/webp',
};

const decodableMimeTypes = new Set(['image/jpeg', 'image/png', 'image/webp', 'image/avif']);

/**
 * Fits every texture within `maxSize` and converts it to the requested format. A texture that already fits and has
 * the right format keeps its original bytes, and a conversion that would grow the file is dropped.
 *
 * @returns Whether any texture had to be downscaled.
 */
export async function encodeTextures(document: Document, encoding: TextureEncoding): Promise<boolean> {
  let downscaled = false;

  for (const texture of document.getRoot().listTextures()) {
    const image = texture.getImage();
    if (image === null) {
      continue;
    }

    if (!decodableMimeTypes.has(texture.getMimeType())) {
      throw new ModelRejectedError(
        'model.texture_unsupported',
        `Texture '${texture.getName()}' is ${texture.getMimeType() || 'of an unknown type'}.`,
      );
    }

    const { width, height, isOpaque } = await describe(texture, image);
    const format = encoding.format(isOpaque);
    const needsResize = Math.max(width, height) > encoding.maxSize;
    downscaled ||= needsResize;

    if (!needsResize && texture.getMimeType() === mimeTypes[format]) {
      continue;
    }

    const encoded = await encode(image, format, encoding.maxSize);
    // A JPEG or PNG that fits decodes everywhere, so converting it is only worth it when the file gets smaller.
    if (!needsResize && isNative(texture) && encoded.byteLength >= image.byteLength) {
      continue;
    }

    texture
      .setImage(encoded)
      .setMimeType(mimeTypes[format])
      .setURI(
        texture.getURI() === ''
          ? ''
          : `${texture.getURI().replace(/\.[^./]+$/, '')}.${format === 'jpeg' ? 'jpg' : format}`,
      );
  }

  updateFormatExtensions(document);
  return downscaled;
}

async function describe(texture: Texture, image: Uint8Array) {
  try {
    const decoder = sharp(image, { failOn: 'error' });
    const [metadata, stats] = await Promise.all([decoder.metadata(), decoder.stats()]);
    return { width: metadata.width, height: metadata.height, isOpaque: stats.isOpaque };
  } catch (error) {
    throw new ModelRejectedError(
      'model.texture_unsupported',
      `Texture '${texture.getName()}' could not be decoded.`,
      { cause: error },
    );
  }
}

function encode(image: Uint8Array, format: TextureFormat, maxSize: number): Promise<Buffer> {
  const resized = sharp(image).resize({
    width: maxSize,
    height: maxSize,
    fit: 'inside',
    withoutEnlargement: true,
  });

  switch (format) {
    case 'jpeg':
      return resized.jpeg({ quality: 88, mozjpeg: true }).toBuffer();
    case 'png':
      return resized.png({ compressionLevel: 9, effort: 8 }).toBuffer();
    case 'webp':
      return resized.webp({ quality: 88, alphaQuality: 95, effort: 5 }).toBuffer();
  }
}

function isNative(texture: Texture): boolean {
  return texture.getMimeType() === 'image/jpeg' || texture.getMimeType() === 'image/png';
}

/** Declares EXT_texture_webp exactly when WebP textures remain, and drops AVIF once none is left. */
function updateFormatExtensions(document: Document): void {
  const root = document.getRoot();
  const mimeTypesInUse = new Set(root.listTextures().map((texture) => texture.getMimeType()));
  const find = (name: string) =>
    root.listExtensionsUsed().find((extension) => extension.extensionName === name);

  const webp = find(EXTTextureWebP.EXTENSION_NAME);
  if (mimeTypesInUse.has('image/webp')) {
    (webp ?? document.createExtension(EXTTextureWebP)).setRequired(true);
  } else {
    webp?.dispose();
  }

  if (!mimeTypesInUse.has('image/avif')) {
    find(EXTTextureAVIF.EXTENSION_NAME)?.dispose();
  }
}
