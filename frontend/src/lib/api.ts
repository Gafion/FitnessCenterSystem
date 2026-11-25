import createClient from 'openapi-fetch';
import type { components, paths } from '@/api/schema';
import { currentLanguage, type Language } from '@/i18n';

// The only way the frontend calls the API (rule 6). Types come from the generated
// schema; run `bun run api:generate` after the backend's OpenAPI document changes.
export const api = createClient<paths>();

// Every request names the language the app is in, so texts the API sends back (the terms,
// the membership type's name) match it, and a signup's emails come in it (D-065).
api.use({
  onRequest({ request }) {
    request.headers.set('Accept-Language', currentLanguage());
    return request;
  },
});

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

/** Asks for a new confirmation email (D-038). The API answers the same whether or not the email has an account. */
export async function resendEmailConfirmation(email: string): Promise<void> {
  await api.POST('/api/identity/confirm-email/resend', { body: { email } });
}

/** The `type` of an API problem answer, e.g. "class-full", or "unexpected". */
export function problemType(error: unknown): string {
  return typeof error === 'object' &&
    error !== null &&
    'type' in error &&
    typeof error.type === 'string'
    ? error.type
    : 'unexpected';
}

/** Whether the simulated pages are on (D-041). Off unless the API says otherwise. */
export async function isSimulationEnabled(): Promise<boolean> {
  const { data } = await api.GET('/api/simulation');
  return data?.enabled ?? false;
}

/**
 * The logged-in member's payments and paid-until date (D-020). Null for an account
 * without a membership.
 */
export async function getMyPayments(): Promise<
  Schemas['MyPayments'] | null | 'logged-out'
> {
  const { data, response } = await api.GET('/api/members/me/payments');
  if (response.status === 401) {
    return 'logged-out';
  }
  if (response.status === 404) {
    return null;
  }
  if (!data) {
    throw new Error('Could not load the payments.');
  }
  return data;
}

/** The logged-in account's membership (D-022). Null for an account without one. */
export async function getMyMembership(): Promise<
  Schemas['MyMembership'] | null
> {
  const { data, response } = await api.GET('/api/members/me');
  if (response.status === 404) {
    return null;
  }
  if (!data) {
    throw new Error('Could not load the membership.');
  }
  return data;
}

/**
 * Cancels the logged-in account's membership with one month's notice to the end of a
 * month (D-048). "already-cancelled" when an earlier cancellation stands; it is kept.
 */
export async function cancelMyMembership(): Promise<
  Schemas['MyMembership'] | 'already-cancelled' | 'logged-out'
> {
  const { data, response } = await api.POST('/api/members/me/cancellation');
  if (response.status === 401) {
    return 'logged-out';
  }
  if (response.status === 409) {
    return 'already-cancelled';
  }
  if (!data) {
    throw new Error('Could not cancel the membership.');
  }
  return data;
}

/**
 * Undoes the pending cancellation, up to and including its effective date (D-055). With
 * nothing pending it changes nothing. "ended" when the effective date has passed.
 */
export async function undoMyCancellation(): Promise<
  Schemas['MyMembership'] | 'ended' | 'logged-out'
> {
  const { data, response } = await api.DELETE('/api/members/me/cancellation');
  if (response.status === 401) {
    return 'logged-out';
  }
  if (response.status === 409) {
    return 'ended';
  }
  if (!data) {
    throw new Error('Could not undo the cancellation.');
  }
  return data;
}

/** The logged-in account's booked classes that haven't started, soonest first (D-022). */
export async function getMyBookings(): Promise<Schemas['MyBooking'][]> {
  const { data } = await api.GET('/api/booking/my-bookings');
  if (!data) {
    throw new Error('Could not load the bookings.');
  }
  return data;
}

/** The spots held for the logged-in account from a waitlist, soonest class first (D-067). */
export async function getMyOffers(): Promise<Schemas['MyOffer'][]> {
  const { data } = await api.GET('/api/booking/my-offers');
  if (!data) {
    throw new Error('Could not load the offers.');
  }
  return data;
}

/** Changes the language of the logged-in account's emails (D-065). */
export async function setPreferredLanguage(language: Language): Promise<void> {
  const { response } = await api.PUT('/api/identity/me/language', {
    body: { language },
  });
  if (!response.ok) {
    throw new Error('Could not save the language.');
  }
}
