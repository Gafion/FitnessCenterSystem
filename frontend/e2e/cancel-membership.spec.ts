import { expect, type Page, test } from '@playwright/test';

// Cancelling and undoing the cancellation from the account page (D-048, D-055, FIT-70, FIT-95) against the built
// frontend, with the API mocked at the network level like account.spec.ts. Synthetic data only.

const account = {
  accountId: '0199a3c0-0000-7000-8000-0000000000aa',
  email: 'test.person@example.com',
  firstName: 'Test',
  lastName: 'Person',
  hasGoogleLogin: false,
  staffRoles: [],
};

const membership = {
  membershipType: 'General membership',
  monthlyPriceOre: 29_900,
  status: 'Active',
  memberSince: '2026-11-23',
  paidUntil: '2026-12-22',
  cancelledAt: null,
  cancellationEffectiveOn: null,
  canUndoCancellation: false,
};

// Cancelled at 09:30 Copenhagen time on 24 Nov: one month's notice to the end of December.
const cancelled = {
  ...membership,
  cancelledAt: '2026-11-24T08:30:00Z',
  cancellationEffectiveOn: '2026-12-31',
  canUndoCancellation: true,
};

async function mockApi(
  page: Page,
  start: typeof membership | typeof cancelled = membership,
) {
  let current: typeof membership | typeof cancelled = start;
  let cancelRequests = 0;
  let undoRequests = 0;

  await page.route('**/api/identity/me', (route) =>
    route.fulfill({ json: account }),
  );
  await page.route('**/api/identity/login-options', (route) =>
    route.fulfill({ json: { google: false } }),
  );
  await page.route('**/api/simulation', (route) =>
    route.fulfill({ json: { enabled: false } }),
  );
  await page.route('**/api/access/cards/mine', (route) =>
    route.fulfill({ json: { uid: 'A1B2C3D4' } }),
  );
  await page.route('**/api/members/me', (route) =>
    route.fulfill({ json: current }),
  );
  await page.route('**/api/members/me/cancellation', (route) => {
    if (route.request().method() === 'DELETE') {
      undoRequests += 1;
      current = membership;
      return route.fulfill({ json: membership });
    }
    cancelRequests += 1;
    current = cancelled;
    return route.fulfill({ json: cancelled });
  });
  await page.route('**/api/members/me/payments', (route) =>
    route.fulfill({
      json: {
        status: 'Active',
        paidUntil: '2026-12-22',
        monthlyPriceOre: 29_900,
        payments: [],
      },
    }),
  );
  await page.route('**/api/booking/my-bookings', (route) =>
    route.fulfill({ json: [] }),
  );

  return {
    cancelRequests: () => cancelRequests,
    undoRequests: () => undoRequests,
  };
}

test('a member cancels with one confirmation and sees the last day of access', async ({
  page,
}) => {
  const api = await mockApi(page);

  await page.goto('/account');
  const section = page.getByRole('region', { name: 'Membership' });

  await section.getByRole('button', { name: 'Cancel membership' }).click();
  await expect(section).toContainText(
    "You have one month's notice to the end of a month.",
  );
  expect(api.cancelRequests()).toBe(0);

  await section
    .getByRole('button', { name: 'Yes, cancel my membership' })
    .click();

  await expect(section.getByRole('status')).toContainText(
    'You have access until 31 Dec 2026',
  );
  await expect(section).toContainText('Cancelled on24 Nov 2026');
  await expect(section).toContainText('Access until31 Dec 2026');
  await expect(
    section.getByRole('button', { name: 'Cancel membership' }),
  ).toHaveCount(0);
  expect(api.cancelRequests()).toBe(1);
});

test('going back from the confirmation does not cancel', async ({ page }) => {
  const api = await mockApi(page);

  await page.goto('/account');
  const section = page.getByRole('region', { name: 'Membership' });

  await section.getByRole('button', { name: 'Cancel membership' }).click();
  await section.getByRole('button', { name: 'Go back' }).click();

  await expect(
    section.getByRole('button', { name: 'Cancel membership' }),
  ).toBeVisible();
  expect(api.cancelRequests()).toBe(0);
});

test('a member undoes a pending cancellation and can cancel again', async ({
  page,
}) => {
  const api = await mockApi(page, cancelled);

  await page.goto('/account');
  const section = page.getByRole('region', { name: 'Membership' });
  await expect(section.getByRole('status')).toContainText(
    'You have access until 31 Dec 2026',
  );

  await section.getByRole('button', { name: 'Undo cancellation' }).click();

  await expect(
    section.getByRole('button', { name: 'Cancel membership' }),
  ).toBeVisible();
  await expect(section.getByRole('status')).toHaveCount(0);
  await expect(section).not.toContainText('Access until');
  expect(api.undoRequests()).toBe(1);
});

test('after the effective date there is nothing to undo', async ({ page }) => {
  const api = await mockApi(page, {
    ...cancelled,
    canUndoCancellation: false,
  });

  await page.goto('/account');
  const section = page.getByRole('region', { name: 'Membership' });

  await expect(section.getByRole('status')).toContainText(
    'You have access until 31 Dec 2026',
  );
  await expect(
    section.getByRole('button', { name: 'Undo cancellation' }),
  ).toHaveCount(0);
  expect(api.undoRequests()).toBe(0);
});
