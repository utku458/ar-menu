import type { MenuEventInput } from '@armenu/api-client';

export type MenuEvent =
  | { readonly type: 'menu_viewed' }
  | { readonly type: 'dish_opened' | 'model_viewed' | 'ar_started'; readonly itemId: string };

export type SendEvents = (events: readonly MenuEventInput[]) => void;

export interface TrackerOptions {
  readonly delayMs?: number;
  readonly schedule?: (callback: () => void, delayMs: number) => unknown;
  readonly cancel?: (handle: unknown) => void;
}

/** The API refuses larger batches (RecordMenuEventsCommandValidator). */
export const maxBatch = 20;

/**
 * Collects what a guest does on one menu and sends it in small batches. Each thing is counted once per page load (a
 * guest reopening a dish is one open), and nothing identifies the guest: no id, no cookie, no storage.
 */
export class MenuEventTracker {
  readonly #send: SendEvents;
  readonly #delayMs: number;
  readonly #schedule: (callback: () => void, delayMs: number) => unknown;
  readonly #cancel: (handle: unknown) => void;
  readonly #seen = new Set<string>();
  #pending: MenuEventInput[] = [];
  #timer: unknown;

  constructor(send: SendEvents, options: TrackerOptions = {}) {
    this.#send = send;
    this.#delayMs = options.delayMs ?? 4_000;
    this.#schedule = options.schedule ?? ((callback, delayMs) => setTimeout(callback, delayMs));
    this.#cancel =
      options.cancel ??
      ((handle) => {
        clearTimeout(handle as ReturnType<typeof setTimeout>);
      });
  }

  track(event: MenuEvent): void {
    const itemId = 'itemId' in event ? event.itemId : null;
    const key = `${event.type}:${itemId ?? ''}`;
    if (this.#seen.has(key)) {
      return;
    }

    this.#seen.add(key);
    this.#pending.push({ type: event.type, itemId });

    if (this.#pending.length >= maxBatch) {
      this.flush();
    } else {
      this.#timer ??= this.#schedule(() => {
        this.flush();
      }, this.#delayMs);
    }
  }

  /** Sends everything collected so far; called when the timer fires and when the page is hidden or left. */
  flush(): void {
    if (this.#timer !== undefined) {
      this.#cancel(this.#timer);
      this.#timer = undefined;
    }

    while (this.#pending.length > 0) {
      this.#send(this.#pending.splice(0, maxBatch));
    }
  }
}
