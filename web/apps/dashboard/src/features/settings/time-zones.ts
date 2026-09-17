import type { Option } from '../../ui/fields.tsx';

/** The IANA time zone this browser runs in, the one sign-up proposes for a new business. */
export function deviceTimeZone(): string {
  return Intl.DateTimeFormat().resolvedOptions().timeZone;
}

/**
 * Every time zone the browser knows, labeled with its current UTC offset ("GMT+3 · Europe/Istanbul") and ordered from
 * west to east, so neighbours are easy to find. The current value is kept even if this browser does not list it.
 */
export function timeZoneOptions(current: string, locale: string, now = new Date()): Option[] {
  const zones = new Set([...Intl.supportedValuesOf('timeZone'), 'UTC', current]);

  return [...zones]
    .map((zone) => ({ zone, offset: offsetMinutes(zone, now) }))
    .sort((left, right) => left.offset - right.offset || left.zone.localeCompare(right.zone, locale))
    .map(({ zone, offset }) => ({
      id: zone,
      label: `${formatOffset(offset)} · ${zone.replaceAll('_', ' ')}`,
    }));
}

/** Minutes ahead of UTC at `now`; daylight saving time included. */
export function offsetMinutes(zone: string, now: Date): number {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: zone,
    hourCycle: 'h23',
    year: 'numeric',
    month: 'numeric',
    day: 'numeric',
    hour: 'numeric',
    minute: 'numeric',
  }).formatToParts(now);
  const part = (type: Intl.DateTimeFormatPartTypes) =>
    Number(parts.find((candidate) => candidate.type === type)?.value);
  const local = Date.UTC(part('year'), part('month') - 1, part('day'), part('hour'), part('minute'));
  return Math.round((local - Math.floor(now.getTime() / 60_000) * 60_000) / 60_000);
}

function formatOffset(minutes: number): string {
  const sign = minutes < 0 ? '−' : '+';
  const hours = Math.floor(Math.abs(minutes) / 60);
  const rest = Math.abs(minutes) % 60;
  return `GMT${sign}${hours}${rest === 0 ? '' : `:${String(rest).padStart(2, '0')}`}`;
}
