// Payment periods are Copenhagen calendar dates (YYYY-MM-DD) with no time of day, so
// they are formatted as-is in UTC: no time zone may move them to another day.
const dateFormat = new Intl.DateTimeFormat('en-GB', {
  timeZone: 'UTC',
  day: 'numeric',
  month: 'short',
  year: 'numeric',
});

/** "2026-11-23" → "23 Nov 2026" */
export function formatDate(date: string): string {
  return dateFormat.format(new Date(`${date}T00:00:00Z`));
}
