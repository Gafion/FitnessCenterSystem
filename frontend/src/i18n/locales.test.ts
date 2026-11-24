import { expect, test } from 'vitest';
import da from './locales/da.json';
import en from './locales/en.json';

function keys(value: unknown, prefix = ''): string[] {
  if (typeof value !== 'object' || value === null) {
    return [prefix];
  }
  return Object.entries(value).flatMap(([key, child]) =>
    keys(child, prefix ? `${prefix}.${key}` : key),
  );
}

test('Danish and English have the same keys', () => {
  expect(keys(da).sort()).toEqual(keys(en).sort());
});

test('every Danish text is filled in', () => {
  for (const key of keys(da)) {
    const text = key
      .split('.')
      .reduce<unknown>(
        (node, part) => (node as Record<string, unknown>)[part],
        da,
      );
    expect(text, key).toEqual(expect.stringMatching(/\S/));
  }
});
