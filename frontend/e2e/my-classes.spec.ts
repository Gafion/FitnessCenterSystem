import { expect, type Page, test } from '@playwright/test';

// The instructor's classes screen (D-049, D-060), against the built frontend with the API
// mocked at the network level. Synthetic data only.

const instructor = {
  accountId: '0199a3c0-0000-7000-8000-0000000000ee',
  email: 'test.instructor@example.com',
  firstName: 'Test',
  lastName: 'Instructor',
  hasGoogleLogin: false,
  twoFactorEnabled: true,
  mustSetUpAuthenticator: false,
  preferredLanguage: 'en',
  staffRoles: ['Instructor'],
};

async function mockMe(page: Page, me: object) {
  await page.route('**/api/identity/me', (route) =>
    route.fulfill({ json: me }),
  );
}

test('an instructor sees their classes and who booked them', async ({
  page,
}) => {
  await mockMe(page, instructor);
  await page.route('**/api/booking/my-classes', (route) =>
    route.fulfill({
      json: [
        {
          classId: '0199a3c0-0000-7000-8000-0000000000c1',
          className: 'Spinning',
          startsAt: '2026-11-24T17:00:00Z',
          endsAt: '2026-11-24T17:45:00Z',
          capacity: 12,
          bookedNames: ['Alex E.', 'Sam S.'],
        },
        {
          classId: '0199a3c0-0000-7000-8000-0000000000c2',
          className: 'Yoga',
          startsAt: '2026-11-25T07:00:00Z',
          endsAt: '2026-11-25T08:00:00Z',
          capacity: 10,
          bookedNames: [],
        },
      ],
    }),
  );

  await page.goto('/staff/my-classes');

  await expect(
    page.getByRole('heading', { name: 'Your classes', level: 1 }),
  ).toBeVisible();
  await expect(
    page.getByRole('heading', { name: 'Tuesday 24 November' }),
  ).toBeVisible();
  const spinning = page.getByRole('listitem', { name: 'Spinning' });
  await expect(spinning).toContainText('18:00–18:45');
  await expect(spinning).toContainText('2 of 12 spots booked');
  await expect(
    spinning
      .getByRole('list', { name: 'Booked for Spinning' })
      .getByRole('listitem'),
  ).toHaveText(['Alex E.', 'Sam S.']);
  const yoga = page.getByRole('listitem', { name: 'Yoga' });
  await expect(yoga).toContainText('08:00–09:00');
  await expect(yoga).toContainText('No one has booked yet.');
});

test('someone without the instructor role is told the page is for instructors', async ({
  page,
}) => {
  await mockMe(page, { ...instructor, staffRoles: ['Manager'] });
  await page.route('**/api/booking/my-classes', (route) =>
    route.fulfill({ status: 403 }),
  );

  await page.goto('/staff/my-classes');

  await expect(
    page.getByText('Only instructors have classes here.'),
  ).toBeVisible();
  await expect(
    page.getByRole('link', { name: 'Back to your account' }),
  ).toBeVisible();
});

test('a logged-out visitor is sent to the login page', async ({ page }) => {
  await page.route('**/api/identity/me', (route) =>
    route.fulfill({ status: 401 }),
  );

  await page.goto('/staff/my-classes');

  await expect(page).toHaveURL(/\/login$/);
});

test('staff without the authenticator set up are sent to set it up', async ({
  page,
}) => {
  await mockMe(page, { ...instructor, mustSetUpAuthenticator: true });

  await page.goto('/staff/my-classes');

  await expect(page).toHaveURL(/\/authenticator$/);
});
