import createClient from 'openapi-fetch';
import type { components, paths } from '@/api/schema';

// The only way the frontend calls the API (rule 6). Types come from the generated
// schema; run `bun run api:generate` after the backend's OpenAPI document changes.
export const api = createClient<paths>();

export type Schemas = components['schemas'];

/** Validation errors by field name, as the API returns them; "" holds errors for no field. */
export type FieldErrors = Record<string, string[]>;

/**
 * Google login starts with a full browser navigation (Google's pages come in between),
 * not a fetch. Typed against the schema so a renamed endpoint fails the typecheck.
 */
export const googleLoginUrl =
  '/api/identity/external/google' satisfies keyof paths;
export const googleLinkUrl =
  '/api/identity/external/google/link' satisfies keyof paths;

export async function getLoginOptions(): Promise<Schemas['LoginOptions']> {
  const { data } = await api.GET('/api/identity/login-options');
  return data ?? { google: false };
}

/** Reads the `error` code the API's Google redirects add to a page's address. */
export function errorSearch(search: Record<string, unknown>): {
  error?: string;
} {
  return typeof search.error === 'string' ? { error: search.error } : {};
}

export async function getSignupTerms(): Promise<Schemas['SignupTerms']> {
  const { data } = await api.GET('/api/members/signup/terms');
  if (!data) {
    throw new Error('Could not load the signup terms.');
  }
  return data;
}
