import { describe, expect, test } from 'vitest';

import { maxBatch, MenuEventTracker } from './menu-events.ts';

function tracker() {
  const batches: unknown[][] = [];
  const timers: (() => void)[] = [];
  const cancelled: unknown[] = [];
  const events = new MenuEventTracker((batch) => batches.push([...batch]), {
    schedule: (callback) => timers.push(callback) - 1,
    cancel: (handle) => cancelled.push(handle),
  });
  return { events, batches, timers, cancelled };
}

describe('MenuEventTracker', () => {
  test('sends what happened in one batch after a pause', () => {
    const { events, batches, timers } = tracker();

    events.track({ type: 'menu_viewed' });
    events.track({ type: 'dish_opened', itemId: 'lahmacun' });
    expect(batches).toEqual([]);
    expect(timers).toHaveLength(1);

    timers[0]?.();

    expect(batches).toEqual([
      [
        { type: 'menu_viewed', itemId: null },
        { type: 'dish_opened', itemId: 'lahmacun' },
      ],
    ]);
  });

  test('counts each thing once per page load', () => {
    const { events, batches } = tracker();

    events.track({ type: 'dish_opened', itemId: 'lahmacun' });
    events.flush();
    events.track({ type: 'dish_opened', itemId: 'lahmacun' });
    events.track({ type: 'ar_started', itemId: 'lahmacun' });
    events.flush();

    expect(batches).toEqual([
      [{ type: 'dish_opened', itemId: 'lahmacun' }],
      [{ type: 'ar_started', itemId: 'lahmacun' }],
    ]);
  });

  test('never sends a batch larger than the API accepts, and sends a full one at once', () => {
    const { events, batches, cancelled } = tracker();

    for (let index = 0; index < maxBatch; index++) {
      events.track({ type: 'dish_opened', itemId: `dish-${String(index)}` });
    }

    expect(batches).toHaveLength(1);
    expect(batches[0]).toHaveLength(maxBatch);
    expect(cancelled).toHaveLength(1);
  });

  test('flushing with nothing collected sends nothing', () => {
    const { events, batches } = tracker();

    events.flush();

    expect(batches).toEqual([]);
  });
});
