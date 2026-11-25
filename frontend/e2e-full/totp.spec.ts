import { expect, test } from '@playwright/test';
import { totp } from './totp';

// RFC 6238 appendix B, SHA-1 rows: the key is the ASCII "12345678901234567890",
// and the 8-digit values there end in these 6 digits (the same truncation, mod 10^6).
// https://www.rfc-editor.org/rfc/rfc6238#appendix-B
const rfcKey = 'GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ';

const vectors: [number, string][] = [
  [59, '287082'],
  [1111111109, '081804'],
  [1111111111, '050471'],
  [1234567890, '005924'],
  [2000000000, '279037'],
  [20000000000, '353130'],
];

for (const [seconds, code] of vectors) {
  test(`TOTP matches RFC 6238 at T=${seconds}`, () => {
    expect(totp(rfcKey, new Date(seconds * 1000))).toBe(code);
  });
}

test('TOTP accepts the key as the setup page shows it', () => {
  const at = new Date(59_000);
  expect(totp('gezd gnbv gy3t qojq gezd gnbv gy3t qojq', at)).toBe(
    totp(rfcKey, at),
  );
});
