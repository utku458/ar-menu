import type { ArViewerControls } from '@armenu/ar-viewer';
import { AnimatePresence, m } from 'motion/react';
import { type RefObject, useState } from 'react';

import { useI18n } from '../../i18n/i18n-context.ts';
import { GlassButton } from '../../ui/glass/GlassButton.tsx';
import { CubeIcon, RecenterIcon, RotateIcon } from '../../ui/icons.tsx';
import { spring } from '../../ui/motion.ts';

export interface ArStageOverlayProps {
  readonly controls: RefObject<ArViewerControls | null>;
  /**
   * `<model-viewer>`'s own verdict, from its load event, rather than our capability probe: it also accounts for the
   * model's AR modes and the element's state, and it is the answer the end-to-end tests assert against.
   */
  readonly canActivateAr: boolean;
  /** Controls stay hidden until the model can actually respond to them. */
  readonly isReady: boolean;
  /** Hidden during an AR handoff, when the launch overlay owns the screen. */
  readonly isHidden: boolean;
  readonly onActivateAr: () => void;
}

/**
 * The floating controls over the 3D stage.
 *
 * This is the overlay the product brief asks for, in the only place the web platform actually gives us one. During a
 * Quick Look or Scene Viewer session the operating system owns every pixel and no React tree is on screen; during a
 * WebXR session `<model-viewer>` owns the canvas. The in-page turntable — which is where guests spend almost all of
 * their time with a dish — is ours, and these are its controls.
 *
 * Laid out as a bottom cluster rather than a ring of buttons in the corners: the stage is square and sits under a
 * dish name and a price, so anything in the upper half covers food, and anything on the far edges is out of reach of
 * the thumb holding the phone.
 */
export function ArStageOverlay({
  controls,
  canActivateAr,
  isReady,
  isHidden,
  onActivateAr,
}: ArStageOverlayProps) {
  const { messages } = useI18n();
  const [isRotating, setIsRotating] = useState(false);

  const toggleRotation = () => {
    const next = !isRotating;
    setIsRotating(next);
    controls.current?.setAutoRotate(next);
  };

  const recenter = () => {
    // Recentring a spinning dish and leaving it spinning puts it straight back off-centre.
    setIsRotating(false);
    controls.current?.setAutoRotate(false);
    controls.current?.resetView();
  };

  return (
    <AnimatePresence>
      {isReady && !isHidden && (
        <m.div
          key="stage-controls"
          // Rises out of the stage's bottom edge, which clips it, instead of fading in: at every frame of the
          // entrance the labels are either fully legible or not on screen at all — never half-transparent text.
          initial={{ y: '100%' }}
          animate={{ y: 0 }}
          exit={{ y: '100%', transition: spring.precise }}
          transition={spring.control}
          // `pointer-events-none` on the rail, re-enabled per control: the rest of the stage stays draggable, so a
          // thumb that lands between two buttons turns the dish instead of hitting dead space.
          className="pointer-events-none absolute inset-x-0 bottom-0 z-10 flex items-center justify-center gap-2 p-4"
        >
          <div className="pointer-events-auto flex items-center gap-2">
            <GlassButton
              surface="media"
              label={messages.autoRotate}
              isPressed={isRotating}
              onPress={toggleRotation}
            >
              <RotateIcon className="size-5" />
            </GlassButton>

            <GlassButton surface="media" label={messages.recenter} onPress={recenter}>
              <RecenterIcon className="size-5" />
            </GlassButton>
          </div>

          {/*
            The primary action, and the only one that carries a label: on a stage full of icons the one thing a guest
            came for should be readable without decoding a glyph. Absent on desktops, which cannot start AR at all;
            there the stage caption invites the guest to open the menu on a phone instead.
          */}
          {canActivateAr && (
            <m.div layout transition={spring.control} className="pointer-events-auto">
              <GlassButton shape="pill" emphasis="brand" onPress={onActivateAr}>
                <CubeIcon className="size-5" />
                {messages.seeOnYourTable}
              </GlassButton>
            </m.div>
          )}
        </m.div>
      )}
    </AnimatePresence>
  );
}
