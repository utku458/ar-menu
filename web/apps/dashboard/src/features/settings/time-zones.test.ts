import { describe, expect, it } from 'vitest';

import { offsetMinutes, timeZoneOptions } from './time-zones.ts';

const september = new Date('2026-09-15T12:00:00Z');

describe('time zones', () => {
  it('knows offsets, half hours and daylight saving time included', () => {
    expect(offsetMinutes('Europe/Istanbul', september)).toBe(180);
    expect(offsetMinutes('Asia/Kolkata', september)).toBe(330);
    expect(offsetMinutes('Europe/Berlin', september)).toBe(120);
    expect(offsetMinutes('Europe/Berlin', new Date('2026-01-15T12:00:00Z'))).toBe(60);
    expect(offsetMinutes('America/New_York', september)).toBe(-240);
  });

  it('labels zones with their offset and orders them from west to east', () => {
    const options = timeZoneOptions('Europe/Istanbul', 'tr', september);
    const index = (zone: string) => options.findIndex((option) => option.id === zone);

    expect(options.find((option) => option.id === 'Europe/Istanbul')?.label).toBe('GMT+3 · Europe/Istanbul');
    expect(options.find((option) => option.id === 'America/New_York')?.label).toBe(
      'GMT−4 · America/New York',
    );
    expect(index('America/New_York')).toBeLessThan(index('UTC'));
    expect(index('UTC')).toBeLessThan(index('Europe/Istanbul'));
  });

  it('keeps a saved zone this browser does not list', () => {
    expect(
      timeZoneOptions('Asia/Calcutta', 'en', september).some((option) => option.id === 'Asia/Calcutta'),
    ).toBe(true);
  });
});
