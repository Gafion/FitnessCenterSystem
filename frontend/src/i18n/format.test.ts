import { expect, test } from 'vitest';
import {
  formatDateTime,
  formatDay,
  formatTime,
} from '@/features/classes/class-time';
import { formatDate } from '@/features/payments/payment-date';
import i18n from '@/i18n';
import { formatOre } from '@/lib/money';

// 05:00 UTC is 06:00 in Copenhagen in November (CET).
const instant = '2026-11-24T05:00:00Z';

test('Danish uses Danish formats: 24-hour time, dd.mm.yyyy and kr.', async () => {
  await i18n.changeLanguage('da');

  expect(formatDay(instant)).toBe('tirsdag 24. november');
  expect(formatTime('2026-11-24T17:00:00Z')).toBe('18.00');
  expect(formatDateTime(instant)).toBe('tirs. 24. nov., 06.00');
  expect(formatDate('2026-11-23')).toBe('23.11.2026');
  expect(formatOre(29_900)).toBe('299,00 kr.');
});

test('English keeps 24-hour time and day-before-month dates', async () => {
  await i18n.changeLanguage('en');

  expect(formatDay(instant)).toBe('Tuesday 24 November');
  expect(formatTime('2026-11-24T17:00:00Z')).toBe('18:00');
  expect(formatDate('2026-11-23')).toBe('23 Nov 2026');
});
