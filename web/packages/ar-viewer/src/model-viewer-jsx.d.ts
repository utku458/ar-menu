import type { ModelViewerElement } from '@google/model-viewer';
import type { DetailedHTMLProps, HTMLAttributes } from 'react';

/** The <model-viewer> attributes this package uses. Values are attribute strings, as in HTML. */
interface ModelViewerAttributes {
  src: string;
  alt: string;
  'ios-src'?: string | undefined;
  poster?: string | undefined;
  ar?: boolean;
  'ar-modes'?: string;
  'ar-scale'?: 'auto' | 'fixed';
  'ar-placement'?: 'floor' | 'wall';
  'xr-environment'?: boolean;
  'camera-controls'?: boolean;
  'camera-orbit'?: string;
  'touch-action'?: 'pan-y' | 'pan-x' | 'none';
  'interaction-prompt'?: 'auto' | 'none';
  'environment-image'?: string;
  'tone-mapping'?: string;
  exposure?: string;
  'shadow-intensity'?: string;
  'shadow-softness'?: string;
  loading?: 'auto' | 'lazy' | 'eager';
  reveal?: 'auto' | 'manual';
}

declare module 'react' {
  namespace JSX {
    interface IntrinsicElements {
      'model-viewer': DetailedHTMLProps<HTMLAttributes<ModelViewerElement>, ModelViewerElement> &
        ModelViewerAttributes;
    }
  }
}
