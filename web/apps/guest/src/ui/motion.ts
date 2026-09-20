import type { Transition } from 'motion/react';

/**
 * Motion tokens.
 *
 * Durations are deliberately absent: every transition here is a spring, described by how it *feels* (how stiff the
 * connection is, how much it settles) rather than how long it takes. A spring interrupted mid-flight continues from
 * its current velocity, which is what makes a sheet that is grabbed, flung and caught again feel like an object
 * instead of an animation — a tweened duration would restart and look broken.
 *
 * `visualDuration` states the perceived settle time; Framer Motion derives stiffness from it, so these read the same
 * on any screen refresh rate.
 */
export const spring = {
  /** Sheets and anything large that travels a long distance. Slightly underdamped: a whisper of overshoot. */
  sheet: { type: 'spring', visualDuration: 0.42, bounce: 0.18 },
  /** Floating controls appearing, badges popping in. Snappier and a touch more playful. */
  control: { type: 'spring', visualDuration: 0.3, bounce: 0.28 },
  /** Anything that must not overshoot: progress, layout shifts, values the eye tracks precisely. */
  precise: { type: 'spring', visualDuration: 0.35, bounce: 0 },
  /** Press feedback. Fast enough to feel like a physical button bottoming out. */
  press: { type: 'spring', visualDuration: 0.16, bounce: 0.2 },
} as const satisfies Record<string, Transition>;

/** Opacity-only cross-fades, where a spring would be imperceptible and a tween is honest. */
export const fade = {
  in: { duration: 0.24, ease: [0.22, 1, 0.36, 1] },
  out: { duration: 0.16, ease: [0.4, 0, 1, 1] },
} as const satisfies Record<string, Transition>;

/**
 * How far past its resting edge a dragged sheet may be pulled. Zero upward (a sheet does not lift off the top of the
 * screen), generous downward (resistance you can feel is what tells a thumb the gesture is being tracked).
 */
export const sheetDragElastic = { top: 0, bottom: 0.55 } as const;

/**
 * The flick test. A sheet closes when it has been dragged past `distance`, *or* thrown faster than `velocity`
 * regardless of distance — the second half is what makes a short, fast flick work, and its absence is the single
 * most common reason a hand-built drawer feels wrong.
 */
export const dismissThreshold = { distance: 120, velocity: 520 } as const;
