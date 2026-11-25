import { expect, type Page } from '@playwright/test';
import { totp } from './totp';

// The synthetic development staff (D-049) with the authenticator on (FIT-109).
// The API seeds them from Identity:DevelopmentStaffPassword and
// Identity:DevelopmentStaffAuthenticatorKey; these tests need the same two
// values. CI sets test-only values in its "Full-stack E2E" job.

export type Staff = { email: string; firstName: string };

export const devAdmin: Staff = {
  email: 'dev-admin@example.com',
  firstName: 'Admin',
};

function required(name: string): string {
  const value = process.env[name];
  if (!value) {
    throw new Error(
      `Set ${name} to the API's development staff setting (see CLAUDE.md).`,
    );
  }
  return value;
}

/** The code the seeded staff's authenticator app shows at `at` (default: now). */
export function staffCode(at?: Date): string {
  return totp(required('E2E_STAFF_AUTHENTICATOR_KEY'), at);
}

/** The password step; the page then asks for the code from the app. */
export async function enterStaffPassword(page: Page, staff: Staff) {
  await page.goto('/login');
  await page.getByLabel('Email').fill(staff.email);
  await page.getByLabel('Password').fill(required('E2E_STAFF_PASSWORD'));
  await page.getByRole('button', { name: 'Log in', exact: true }).click();
}

export async function enterCode(page: Page, code: string) {
  await page.getByLabel('Code from the app').fill(code);
  await page.getByRole('button', { name: 'Continue' }).click();
}

/** Password, then the code from the app: a full staff login, on the account page. */
export async function logInAsStaff(page: Page, staff: Staff) {
  await enterStaffPassword(page, staff);
  await enterCode(page, staffCode());
  await expect(
    page.getByRole('heading', { name: `Welcome, ${staff.firstName}` }),
  ).toBeVisible();
}
