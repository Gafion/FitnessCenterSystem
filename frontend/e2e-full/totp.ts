import { createHmac } from 'node:crypto';

// Plays the authenticator app (D-059): a plain RFC 6238 TOTP generator
// (HMAC-SHA1, 30-second steps, 6 digits), written from the RFC rather than
// taken from a library, like TestAuthenticator in the integration tests.
// https://www.rfc-editor.org/rfc/rfc6238

const alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567';

/** RFC 4648 Base32, as the setup key is shown: spaces and any case allowed. */
function base32Decode(text: string): Buffer {
  const clean = text.replace(/\s/g, '').replace(/=+$/, '').toUpperCase();
  const bytes: number[] = [];
  let buffer = 0;
  let bits = 0;
  for (const char of clean) {
    const value = alphabet.indexOf(char);
    if (value < 0) throw new Error(`Not a Base32 key: "${char}"`);
    buffer = ((buffer << 5) | value) & 0xffff;
    bits += 5;
    if (bits >= 8) {
      bits -= 8;
      bytes.push((buffer >> bits) & 0xff);
    }
  }
  return Buffer.from(bytes);
}

/** The code an authenticator app shows for `key` at `at` (default: now). */
export function totp(key: string, at: Date = new Date()): string {
  const counter = Buffer.alloc(8);
  counter.writeBigUInt64BE(BigInt(Math.floor(at.getTime() / 1000 / 30)));
  const hash = createHmac('sha1', base32Decode(key)).update(counter).digest();
  const offset = hash[hash.length - 1] & 0x0f;
  const binary = hash.readUInt32BE(offset) & 0x7fffffff;
  return String(binary % 1_000_000).padStart(6, '0');
}
