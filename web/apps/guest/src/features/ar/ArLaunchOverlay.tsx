import { AnimatePresence, m } from 'motion/react';

import { useI18n } from '../../i18n/i18n-context.ts';
import { ScanFrame } from '../../ui/glass/ScanFrame.tsx';
import { fade, spring } from '../../ui/motion.ts';
import type { ArPath } from './ar-capability.ts';

/**
 * Where the guest is in the handoff to augmented reality.
 *
 * `handingOff` and `searching` are separate states on purpose. They look similar but mean opposite things: one says
 * "another app is about to take over this screen", the other says "you are in it, move your phone". Collapsing them
 * into a single spinner is what makes AR handoffs feel like a crash.
 */
export type ArLaunchPhase =
  /** Nothing to show: the guest has not asked for AR. */
  | { readonly kind: 'idle' }
  /** The tap landed; the session is being negotiated (WebXR permission prompt, or another app opening). */
  | { readonly kind: 'handingOff' }
  /** A WebXR session is live and hunting for a surface. */
  | { readonly kind: 'searching' }
  /** The dish is on the table. The overlay bows out. */
  | { readonly kind: 'placed' }
  /** The session could not start, or ended badly. */
  | { readonly kind: 'failed' };

export interface ArLaunchOverlayProps {
  readonly phase: ArLaunchPhase;
  readonly path: ArPath;
  readonly dishName: string;
  readonly onDismiss: () => void;
}

/**
 * The full-bleed overlay covering the 3D stage during the moments around an AR launch.
 *
 * It is a status region, not a dialog: it never traps focus and never steals it. During a handoff the guest's
 * attention belongs to the browser's own camera prompt or to the app taking over the screen, and a focus trap
 * fighting a system prompt is a trap you cannot escape. `aria-live="polite"` means the same words a sighted guest
 * reads are announced, once, without interrupting.
 */
export function ArLaunchOverlay({ phase, path, dishName, onDismiss }: ArLaunchOverlayProps) {
  return (
    // Always mounted, so it is still here to play the exit animation when its content goes away.
    <AnimatePresence>
      {phase.kind !== 'idle' && phase.kind !== 'placed' && (
        <LaunchPanel
          key="ar-launch"
          kind={phase.kind}
          path={path}
          dishName={dishName}
          onDismiss={onDismiss}
        />
      )}
    </AnimatePresence>
  );
}

function LaunchPanel({
  kind,
  path,
  dishName,
  onDismiss,
}: {
  kind: 'handingOff' | 'searching' | 'failed';
  path: ArPath;
  dishName: string;
  onDismiss: () => void;
}) {
  const { messages } = useI18n();
  const copy = describe(kind, path, messages, dishName);

  return (
    <m.div
      data-over-media=""
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0, transition: fade.out }}
      transition={fade.in}
      className="absolute inset-0 z-20 grid place-items-center overflow-hidden bg-black/55 text-white backdrop-blur-md"
    >
      {kind === 'searching' && <ScanFrame state="searching" />}

      <m.div
        initial={{ opacity: 0, y: 12, scale: 0.97 }}
        animate={{ opacity: 1, y: 0, scale: 1 }}
        transition={spring.control}
        className="relative z-10 flex max-w-[22rem] flex-col items-center gap-3 px-8 text-center"
      >
        {kind !== 'failed' && <PulseRing />}

        <p aria-live="polite" className="text-lg leading-snug font-semibold text-balance">
          {copy.title}
        </p>
        <p className="text-sm leading-relaxed text-balance text-white/75">{copy.body}</p>

        {kind === 'failed' && (
          <button
            type="button"
            onClick={onDismiss}
            className="mt-2 h-11 rounded-full glass-over-media px-6 text-sm font-semibold"
          >
            {messages.close}
          </button>
        )}
      </m.div>
    </m.div>
  );
}

/**
 * A quiet indeterminate indicator: two rings expanding out of a dot.
 *
 * Deliberately not a spinner. A spinner says "waiting for a server"; this says "the device is sensing", which is
 * what is actually happening, and it keeps working as ambience if the handoff takes four seconds on a cold start.
 */
function PulseRing() {
  return (
    <div aria-hidden="true" className="relative mb-1 grid size-12 place-items-center">
      {[0, 0.6].map((delay) => (
        <m.span
          key={delay}
          initial={{ scale: 0.35, opacity: 0.7 }}
          animate={{ scale: 1, opacity: 0 }}
          transition={{ duration: 1.6, repeat: Infinity, delay, ease: 'easeOut' }}
          className="absolute size-12 rounded-full border border-white/70"
        />
      ))}
      <span className="size-2.5 rounded-full bg-white" />
    </div>
  );
}

type Messages = ReturnType<typeof useI18n>['messages'];

/**
 * The words for each phase.
 *
 * The handoff copy branches on the path because the promise differs: a WebXR guest is told a permission prompt is
 * coming and stays here, while a Quick Look or Scene Viewer guest is told their phone is taking over — so that the
 * app disappearing reads as expected rather than as a failure.
 */
function describe(
  kind: 'handingOff' | 'searching' | 'failed',
  path: ArPath,
  messages: Messages,
  dishName: string,
): { readonly title: string; readonly body: string } {
  if (kind === 'failed') {
    return { title: messages.arFailed, body: messages.arFailedBody };
  }
  if (kind === 'searching') {
    return { title: messages.arPointAtTable, body: messages.arPointAtTableBody };
  }
  return path === 'webxr'
    ? { title: messages.arStarting, body: messages.arAllowCamera }
    : { title: messages.arOpening(dishName), body: messages.arHandoffBody };
}
