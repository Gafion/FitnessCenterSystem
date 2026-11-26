import { expect, test } from 'vitest';
import {
  formatDateTime,
  formatDay,
  formatTime,
  formatWithColon,
} from '@/features/classes/class-time';
import { formatDate } from '@/features/payments/payment-date';
import i18n from '@/i18n';
import { formatOre } from '@/lib/money';

// 05:00 UTC is 06:00 in Copenhagen in November (CET).
const instant = '2026-11-24T05:00:00Z';

test('Danish uses Danish formats: 24-hour time with a colon, dd.mm.yyyy and kr.', async () => {
  await i18n.changeLanguage('da');

  expect(formatDay(instant)).toBe('tirsdag 24. november');
  expect(formatTime('2026-11-24T17:00:00Z')).toBe('18:00');
  expect(formatDateTime(instant)).toBe('tirs. 24. nov., 06:00');
  expect(formatDate('2026-11-23')).toBe('23.11.2026');
  expect(formatOre(29_900)).toBe('299,00 kr.');
});

test('English keeps 24-hour time and day-before-month dates', async () => {
  await i18n.changeLanguage('en');

  expect(formatDay(instant)).toBe('Tuesday 24 November');
  expect(formatTime('2026-11-24T17:00:00Z')).toBe('18:00');
  expect(formatDateTime(instant)).toBe('Tue 24 Nov, 06:00');
  expect(formatDate('2026-11-23')).toBe('23 Nov 2026');
});

// D-070: "18:00", not da-DK's "18.00", in every formatter, also Intl's own styles.
test.each([
  ['da-DK', '24. nov. 2026, 18:00'],
  ['en-GB', '24 Nov 2026, 18:00'],
])('a date and time in %s uses a colon', (locale, expected) => {
  const formatter = new Intl.DateTimeFormat(locale, {
    dateStyle: 'medium',
    timeStyle: 'short',
    timeZone: 'Europe/Copenhagen',
  });

  expect(formatWithColon(formatter, new Date('2026-11-24T17:00:00Z'))).toBe(
    expected,
  );
});

test('a colon replaces only the separator between hours and minutes', () => {
  const formatter = new Intl.DateTimeFormat('da-DK', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    timeZone: 'Europe/Copenhagen',
  });

  expect(formatWithColon(formatter, new Date('2026-11-24T17:00:00Z'))).toBe(
    '24.11.2026, 18:00',
  );
});
