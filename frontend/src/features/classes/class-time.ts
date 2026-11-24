import { dateLocale } from '@/i18n';

// Class times are business rules in Europe/Copenhagen (CLAUDE.md), so the page shows
// them in that zone whatever the visitor's device is set to. Dates and times follow
// the UI language (D-065), so the formats are built when they are used.
const timeZone = 'Europe/Copenhagen';

const dayKeyFormat = new Intl.DateTimeFormat('en-CA', {
  timeZone,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
});
const dayOptions: Intl.DateTimeFormatOptions = {
  timeZone,
  weekday: 'long',
  day: 'numeric',
  month: 'long',
};
const timeOptions: Intl.DateTimeFormatOptions = {
  timeZone,
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
};
const dateTimeOptions: Intl.DateTimeFormatOptions = {
  ...dayOptions,
  ...timeOptions,
  weekday: 'short',
  month: 'short',
};

/** The Copenhagen calendar day of an instant, as YYYY-MM-DD. */
function dayKey(instant: string): string {
  return dayKeyFormat.format(new Date(instant));
}

function format(instant: string, options: Intl.DateTimeFormatOptions): string {
  return new Intl.DateTimeFormat(dateLocale(), options).format(
    new Date(instant),
  );
}

/** "tirsdag 24. november" / "Monday 24 November" */
export function formatDay(instant: string): string {
  return format(instant, dayOptions);
}

/** "18.00" / "18:00" */
export function formatTime(instant: string): string {
  return format(instant, timeOptions);
}

/** "tirs. 24. nov., 06.00" / "Mon 24 Nov, 06:00" */
export function formatDateTime(instant: string): string {
  return format(instant, dateTimeOptions);
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
