import type { Plugin } from 'vite';

/** The debug statements <model-viewer> 4.3.1 ships in its base element, recognized by their message. */
const debugLog =
  /console\.log\((?:`|')(?:\[onExtraModelChanged\]|IntersectionObserver fired!|\[\$updateSource\])/g;

/**
 * Removes the debug logging <model-viewer> 4.3.1 left in `model-viewer-base.js`: several lines on every guest's console
 * each time a model loads. Only those statements, only in that file; its warnings and errors stay. Drop the plugin
 * when an upgrade no longer contains them (the build fails when the file has none left to remove).
 */
export function stripModelViewerDebugLogs(): Plugin {
  return {
    name: 'armenu:strip-model-viewer-debug-logs',
    transform(code, id) {
      if (!id.replaceAll('\\', '/').endsWith('/@google/model-viewer/lib/model-viewer-base.js')) {
        return null;
      }

      const stripped = code.replace(debugLog, (match) => match.replace('console.log(', 'void ('));
      if (stripped === code) {
        this.error('No <model-viewer> debug logs left to strip: remove stripModelViewerDebugLogs().');
      }

      return { code: stripped, map: null };
    },
  };
}
