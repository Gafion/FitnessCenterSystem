import { expect, test } from 'vitest';
import { formatDate } from './payment-date';

test('formats a calendar date without moving it to another day', () => {
  expect(formatDate('2026-11-23')).toBe('23 Nov 2026');
  expect(formatDate('2027-01-01')).toBe('1 Jan 2027');
});
