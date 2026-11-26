import { expect, test } from 'vitest';
import { fromCopenhagen, toCopenhagen } from './copenhagen-time';

test('reads an instant as Copenhagen wall-clock time, winter and summer', () => {
  expect(toCopenhagen('2026-11-24T17:00:00Z')).toEqual({
    date: '2026-11-24',
    time: '18:00',
  });
  expect(toCopenhagen('2027-07-01T16:00:00Z')).toEqual({
    date: '2027-07-01',
    time: '18:00',
  });
  // Late evening UTC is the next day in Copenhagen.
  expect(toCopenhagen('2026-12-31T23:30:00Z')).toEqual({
    date: '2027-01-01',
    time: '00:30',
  });
});

test('turns a Copenhagen date and time into a UTC instant', () => {
  expect(fromCopenhagen({ date: '2026-11-24', time: '18:00' })).toBe(
    '2026-11-24T17:00:00.000Z',
  );
  expect(fromCopenhagen({ date: '2027-07-01', time: '18:00' })).toBe(
    '2027-07-01T16:00:00.000Z',
  );
});

test('handles the clock changes', () => {
  // 28 March 2027: 02:00 jumps to 03:00. 02:30 does not exist; it becomes 03:30.
  expect(fromCopenhagen({ date: '2027-03-28', time: '02:30' })).toBe(
    '2027-03-28T01:30:00.000Z',
  );
  expect(fromCopenhagen({ date: '2027-03-28', time: '03:30' })).toBe(
    '2027-03-28T01:30:00.000Z',
  );
  // 31 October 2027: 02:30 happens twice; the first (summer time) is taken.
  expect(fromCopenhagen({ date: '2027-10-31', time: '02:30' })).toBe(
    '2027-10-31T00:30:00.000Z',
  );
});

function expectRoundTrip(from: string, hours: number, stepMinutes: number) {
  const start = Date.parse(from);
  for (
    let at = start;
    at < start + hours * 3_600_000;
    at += stepMinutes * 60_000
  ) {
    const instant = new Date(at).toISOString();
    const local = toCopenhagen(instant);
    const back = fromCopenhagen(local);
    // In the repeated autumn hour both instants read the same; the earlier is returned.
    expect(back && toCopenhagen(back)).toEqual(local);
  }
}

test('round-trips every hour of a year', () => {
  expectRoundTrip('2027-01-01T00:00:00Z', 365 * 24, 60);
});

test('round-trips every quarter hour of both clock-change days', () => {
  expectRoundTrip('2027-03-27T22:00:00Z', 24, 15);
  expectRoundTrip('2027-10-30T22:00:00Z', 24, 15);
});

test('rejects fields that are not a date and a time', () => {
  expect(fromCopenhagen({ date: '', time: '18:00' })).toBeNull();
  expect(fromCopenhagen({ date: '2026-11-24', time: '' })).toBeNull();
  expect(fromCopenhagen({ date: '2026-13-40', time: '18:00' })).toBeNull();
});
