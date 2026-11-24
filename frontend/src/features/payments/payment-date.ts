import { dateLocale } from '@/i18n';

// Payment periods are Copenhagen calendar dates (YYYY-MM-DD) with no time of day, so
// they are formatted as-is in UTC: no time zone may move them to another day.
// Danish shows dd.mm.yyyy and English "23 Nov 2026" (D-065).
const options: Record<'da-DK' | 'en-GB', Intl.DateTimeFormatOptions> = {
  'da-DK': { day: '2-digit', month: '2-digit', year: 'numeric' },
  'en-GB': { day: 'numeric', month: 'short', year: 'numeric' },
};

/** Date options for the current language, in the given time zone. */
export function dateOptions(timeZone: string): Intl.DateTimeFormatOptions {
  return { ...options[dateLocale()], timeZone };
}

/** "2026-11-23" → "23.11.2026" in Danish, "23 Nov 2026" in English */
export function formatDate(date: string): string {
  return new Intl.DateTimeFormat(dateLocale(), dateOptions('UTC')).format(
    new Date(`${date}T00:00:00Z`),
  );
}
