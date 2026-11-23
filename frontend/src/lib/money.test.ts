import { expect, test } from 'vitest';
import { formatOre } from './money';

// Intl puts a no-break space between the amount and "kr.".
test('formats øre as kroner with two decimals', () => {
  expect(formatOre(29_900)).toBe('299,00\u00a0kr.');
  expect(formatOre(5)).toBe('0,05\u00a0kr.');
});
