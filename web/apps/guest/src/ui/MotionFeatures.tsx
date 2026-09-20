import { domMax, LazyMotion, MotionConfig } from 'motion/react';
import type { ReactNode } from 'react';

/**
 * Wraps the animated part of the app: today, the dish sheet.
 *
 * Framer Motion is kept off the startup path by *where* it is imported, not by how: this component only renders
 * inside the dish sheet, which is its own lazy chunk (see features/menu/load-item-sheet.ts), so the features load
 * with the sheet in a single download. Loading them lazily a second time from inside that chunk would only add a
 * request waterfall, during which every `m` component would sit frozen at its `initial` styles — an invisible sheet.
 *
 * `m` + `LazyMotion strict` is kept anyway, as a guard rather than an optimisation: `strict` makes a `motion.*`
 * component throw, and lint forbids importing `motion` at all (see `no-restricted-imports` in eslint.config.js). A
 * `motion.div` added to a startup component would bundle every feature into the JavaScript that stands between a
 * QR scan and a readable menu; an `m.div` there would merely not animate, which is the failure you want.
 *
 * `domMax` rather than the smaller `domAnimation`: the sheet is dragged and the AR pill animates its layout, and
 * `domAnimation` has neither — both would silently do nothing.
 *
 * `reducedMotion="user"` is the accessibility contract: every spring degrades to an instant state change for guests
 * who asked their system for less motion, without a single conditional in the components themselves. Dishes on a
 * table are exactly the audience — vestibular sensitivity and a moving 3D viewport are a bad pairing.
 */
export function MotionFeatures({ children }: { readonly children: ReactNode }) {
  return (
    <LazyMotion features={domMax} strict>
      <MotionConfig reducedMotion="user">{children}</MotionConfig>
    </LazyMotion>
  );
}
