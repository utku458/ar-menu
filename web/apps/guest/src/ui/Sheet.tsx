import { AnimatePresence, m, type PanInfo } from 'motion/react';
import { type ReactNode, useEffect, useRef, useState } from 'react';

import { cx } from './cx.ts';
import { dismissThreshold, fade, sheetDragElastic, spring } from './motion.ts';

export interface SheetProps {
  readonly isOpen: boolean;
  /** Id of the element naming the sheet, wired to `aria-labelledby`. */
  readonly labelledBy: string;
  readonly onClose: () => void;
  readonly className?: string | undefined;
  readonly children?: ReactNode;
}

/**
 * A bottom sheet you can throw away with your thumb.
 *
 * ## Why a native `<dialog>` underneath
 *
 * Everything hard about a modal — trapping focus, making the page behind it inert, the Escape key, the top layer
 * that escapes every ancestor's `overflow` and `z-index` — is already in the browser, correct, and free. A drawer
 * built out of a `<div>` and a focus-trap library reimplements all of it, worse. So the `<dialog>` stays, and
 * Framer Motion animates a panel *inside* it. The dialog itself is a transparent full-screen shell.
 *
 * Two consequences fall out of that split, both deliberate:
 *
 *   - **The backdrop is ours, not `::backdrop`.** The native pseudo-element cannot be driven by a motion value, and
 *     a backdrop that snaps while the sheet springs is the tell that a drawer is fake.
 *   - **Closing is deferred.** `cancel` is intercepted so Escape does not tear the sheet off the screen; the exit
 *     animation runs first and `close()` is called when it finishes, which is also what keeps the browser's own
 *     focus restoration pointing at the card the guest opened.
 *
 * ## Why drag is conditional
 *
 * A sheet that drags from anywhere fights its own scrollbar: the gesture to read further down the dish and the
 * gesture to dismiss it are the same downward swipe. Drag is therefore live only while the content is scrolled to
 * the top — the one moment where pulling down cannot mean "scroll up" — and always live on the grab handle.
 */
export function Sheet({ isOpen, labelledBy, onClose, className, children }: SheetProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const [isAtTop, setIsAtTop] = useState(true);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (dialog !== null && isOpen && !dialog.open) {
      dialog.showModal();
      setIsAtTop(true);
    }
  }, [isOpen]);

  /** Distance and speed both count: a short, fast flick dismisses, a long, slow drag springs back. */
  const handleDragEnd = (_event: unknown, info: PanInfo) => {
    if (info.offset.y > dismissThreshold.distance || info.velocity.y > dismissThreshold.velocity) {
      onClose();
    }
  };

  return (
    <dialog
      ref={dialogRef}
      aria-labelledby={labelledBy}
      // Escape, and the browser's own dismissal: run the exit animation instead of vanishing.
      onCancel={(event) => {
        event.preventDefault();
        onClose();
      }}
      // The dialog element fills the screen and is invisible; the panel inside it is the sheet.
      className="m-0 size-full max-h-none max-w-none bg-transparent p-0 backdrop:bg-transparent"
    >
      <AnimatePresence
        onExitComplete={() => {
          dialogRef.current?.close();
        }}
      >
        {isOpen && (
          <div className="flex size-full flex-col justify-end">
            {/*
              Backdrop clicks are a pointer shortcut, not the only way out: Escape and the close button cover
              keyboards, so this stays a plain presentational layer rather than a button nobody can see.
            */}
            <m.div
              key="scrim"
              onClick={onClose}
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0, transition: fade.out }}
              transition={fade.in}
              className="absolute inset-0 bg-black/55 backdrop-blur-[2px]"
            />

            <m.div
              key="panel"
              drag={isAtTop ? 'y' : false}
              dragConstraints={{ top: 0, bottom: 0 }}
              dragElastic={sheetDragElastic}
              dragMomentum={false}
              onDragEnd={handleDragEnd}
              initial={{ y: '100%' }}
              animate={{ y: 0 }}
              exit={{ y: '100%', transition: spring.sheet }}
              transition={spring.sheet}
              className={cx(
                'relative flex max-h-[92dvh] w-full flex-col rounded-t-sheet bg-surface text-ink shadow-sheet',
                // A sheet that stops short of the full width on a tablet reads as a card, which is what it is there.
                'sm:mx-auto sm:mb-4 sm:max-w-lg sm:rounded-sheet',
                className,
              )}
            >
              {/*
                The grab handle. `touch-none` hands the gesture to the drag listener rather than the browser's own
                scrolling, so the sheet follows the thumb from the very first pixel instead of after a threshold.
              */}
              <div aria-hidden="true" className="flex shrink-0 touch-none justify-center pt-3 pb-1">
                <span className="h-1 w-10 rounded-full bg-ink/20" />
              </div>

              <div
                onScroll={(event) => {
                  setIsAtTop(event.currentTarget.scrollTop <= 0);
                }}
                className="min-h-0 flex-1 overflow-y-auto overscroll-contain"
              >
                {children}
              </div>
            </m.div>
          </div>
        )}
      </AnimatePresence>
    </dialog>
  );
}
