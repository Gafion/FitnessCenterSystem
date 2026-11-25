import { expect, type Page, test } from '@playwright/test';

// The account page's sections (D-022) against the built frontend, with the API mocked
// at the network level like payments.spec.ts. Synthetic data only.

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
};

const booking = {
  classId: '0199a3c0-0000-7000-8000-0000000000c1',
  className: 'Spinning',
  instructorName: 'Test Instructor',
  startsAt: '2026-11-24T17:00:00Z',
  endsAt: '2026-11-24T17:45:00Z',
  cancelUntil: '2026-11-24T05:00:00Z',
};

async function mockApi(
  page: Page,
  options: { bookings: (typeof booking)[]; membershipStatus?: number },
) {
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
    options.membershipStatus
      ? route.fulfill({ status: options.membershipStatus, json: {} })
      : route.fulfill({ json: membership }),
  );
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
    route.fulfill({ json: options.bookings }),
  );
}

test('shows profile, membership, upcoming classes, payments and card', async ({
  page,
}) => {
  await mockApi(page, { bookings: [booking] });

  await page.goto('/account');

  const profile = page.getByRole('region', { name: 'Profile' });
  await expect(profile).toContainText('Test Person');
  await expect(profile).toContainText('test.person@example.com');

  const membershipSection = page.getByRole('region', { name: 'Membership' });
  await expect(membershipSection).toContainText('General membership');
  await expect(membershipSection).toContainText('Active');
  await expect(membershipSection).toContainText('Member since23 Nov 2026');
  await expect(membershipSection).toContainText('Paid until22 Dec 2026');

  const bookings = page.getByRole('region', { name: 'Upcoming classes' });
  await expect(bookings.getByRole('listitem')).toHaveCount(1);
  await expect(bookings).toContainText('Tue 24 Nov, 18:00–18:45, Danish time');
  await bookings.getByRole('link', { name: 'Book a class' }).click();
  await expect(page).toHaveURL(/\/classes$/);
});

test('the page stays up when the membership fails to load', async ({
  page,
}) => {
  await mockApi(page, { bookings: [], membershipStatus: 500 });

  await page.goto('/account');

  await expect(
    page.getByText(
      'Your membership could not be loaded. Please try again later.',
    ),
  ).toBeVisible();
  await expect(page.getByText('You have no upcoming classes.')).toBeVisible();
  await expect(page.getByRole('region', { name: 'Access card' })).toContainText(
    'A1B2C3D4',
  );
});
