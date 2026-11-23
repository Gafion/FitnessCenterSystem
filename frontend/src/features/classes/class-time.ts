// Class times are business rules in Europe/Copenhagen (CLAUDE.md), so the page shows
// them in that zone whatever the visitor's device is set to.
const timeZone = 'Europe/Copenhagen';

const dayKeyFormat = new Intl.DateTimeFormat('en-CA', {
  timeZone,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
});
const dayFormat = new Intl.DateTimeFormat('en-GB', {
  timeZone,
  weekday: 'long',
  day: 'numeric',
  month: 'long',
});
const timeFormat = new Intl.DateTimeFormat('en-GB', {
  timeZone,
  hour: '2-digit',
  minute: '2-digit',
});
const dateTimeFormat = new Intl.DateTimeFormat('en-GB', {
  timeZone,
  weekday: 'short',
  day: 'numeric',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit',
});

/** The Copenhagen calendar day of an instant, as YYYY-MM-DD. */
function dayKey(instant: string): string {
  return dayKeyFormat.format(new Date(instant));
}

/** "Monday 24 November" */
export function formatDay(instant: string): string {
  return dayFormat.format(new Date(instant));
}

/** "18:00" */
export function formatTime(instant: string): string {
  return timeFormat.format(new Date(instant));
}

/** "Mon 24 Nov, 06:00" */
export function formatDateTime(instant: string): string {
  return dateTimeFormat.format(new Date(instant));
}

/** Groups entries by Copenhagen day, keeping their order (the API sends them soonest first). */
export function groupByDay<T extends { startsAt: string }>(
  entries: readonly T[],
): { day: string; entries: T[] }[] {
  const days = new Map<string, T[]>();
  for (const entry of entries) {
    const key = dayKey(entry.startsAt);
    days.set(key, [...(days.get(key) ?? []), entry]);
  }
  return [...days].map(([day, dayEntries]) => ({ day, entries: dayEntries }));
}
