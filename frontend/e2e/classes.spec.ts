import { expect, type Page, test } from '@playwright/test';

// The classes page against the built frontend with the API mocked at the network
// level, like signup.spec.ts. Booking rules themselves are covered by the backend's
// integration tests (BookingTests).

// A device outside Denmark: the page must still show Copenhagen times (D-039).
test.use({ timezoneId: 'America/New_York' });

const now = new Date('2026-11-23T12:00:00Z');

const spinning = {
  classId: '0199a3c0-0000-7000-8000-000000000c01',
  className: 'Spinning',
  instructorName: 'Test Instructor',
  // Tuesday 24 November, 18:00–18:45 in Copenhagen.
  startsAt: '2026-11-24T17:00:00Z',
  endsAt: '2026-11-24T17:45:00Z',
  capacity: 12,
  spotsLeft: 5,
  bookedByMe: false,
  cancelUntil: '2026-11-24T05:00:00Z',
};

const yoga = {
  ...spinning,
  classId: '0199a3c0-0000-7000-8000-000000000c02',
  className: 'Yoga',
  // Wednesday 25 November, 07:00–08:00 in Copenhagen.
  startsAt: '2026-11-25T06:00:00Z',
  endsAt: '2026-11-25T07:00:00Z',
  cancelUntil: '2026-11-24T18:00:00Z',
  spotsLeft: 0,
};

const account = {
  accountId: '0199a3c0-0000-7000-8000-0000000000aa',
  email: 'test.person@example.com',
  firstName: 'Test',
  lastName: 'Person',
  hasGoogleLogin: false,
};

async function mockApi(
  page: Page,
  options: { signedIn: boolean; schedule: () => unknown[] },
) {
  await page.clock.setFixedTime(now);
  await page.route('**/api/booking/schedule', (route) =>
    route.fulfill({ json: options.schedule() }),
  );
  await page.route('**/api/identity/me', (route) =>
    options.signedIn
      ? route.fulfill({ json: account })
      : route.fulfill({ status: 401 }),
  );
  await page.route('**/api/identity/login-options', (route) =>
    route.fulfill({ json: { google: false } }),
  );
}

test('the landing page links to the schedule, grouped by Copenhagen day', async ({
  page,
}) => {
  await mockApi(page, { signedIn: false, schedule: () => [spinning, yoga] });

  await page.goto('/');
  await page.getByRole('link', { name: 'See classes' }).click();

  await expect(page.getByRole('heading', { name: 'Classes' })).toBeVisible();
  const tuesday = page.getByRole('region', { name: 'Tuesday 24 November' });
  await expect(tuesday.getByText('Spinning')).toBeVisible();
  await expect(tuesday.getByText('18:00–18:45')).toBeVisible();
  await expect(tuesday.getByText('with Test Instructor')).toBeVisible();
  await expect(tuesday.getByText('5 spots left')).toBeVisible();
  const wednesday = page.getByRole('region', {
    name: 'Wednesday 25 November',
  });
  await expect(wednesday.getByText('07:00–08:00')).toBeVisible();
  await expect(wednesday.getByText('This class is full.')).toBeVisible();
  await expect(
    wednesday.getByRole('button', { name: 'Book', exact: true }),
  ).toBeDisabled();
});

test('a visitor who is not logged in is sent to login to book', async ({
  page,
}) => {
  let posted = false;
  await mockApi(page, { signedIn: false, schedule: () => [spinning] });
  await page.route('**/api/booking/classes/*/booking', async (route) => {
    posted = true;
    await route.fulfill({ status: 401 });
  });

  await page.goto('/classes');
  await page.getByRole('button', { name: 'Book', exact: true }).click();

  await expect(page.getByRole('heading', { name: 'Log in' })).toBeVisible();
  expect(posted).toBe(false);
});

test('a member books a spot and cancels it again', async ({ page }) => {
  let booked = false;
  const calls: string[] = [];
  await mockApi(page, {
    signedIn: true,
    schedule: () => [
      { ...spinning, bookedByMe: booked, spotsLeft: booked ? 4 : 5 },
    ],
  });
  await page.route(
    `**/api/booking/classes/${spinning.classId}/booking`,
    (route) => {
      calls.push(route.request().method());
      booked = route.request().method() === 'POST';
      return route.fulfill({ status: 204 });
    },
  );

  await page.goto('/account');
  await page.getByRole('link', { name: 'Book a class' }).click();
  await page.getByRole('button', { name: 'Book', exact: true }).click();

  await expect(page.getByText("You're booked.")).toBeVisible();
  await expect(page.getByText('4 spots left')).toBeVisible();
  await expect(
    page.getByText('You can cancel until Tue 24 Nov, 06:00.'),
  ).toBeVisible();

  await page.getByRole('button', { name: 'Cancel booking' }).click();

  await expect(
    page.getByRole('button', { name: 'Book', exact: true }),
  ).toBeVisible();
  await expect(page.getByText('5 spots left')).toBeVisible();
  expect(calls).toEqual(['POST', 'DELETE']);
});

test('a class that filled up meanwhile says it is full', async ({ page }) => {
  await mockApi(page, { signedIn: true, schedule: () => [spinning] });
  await page.route('**/api/booking/classes/*/booking', (route) =>
    route.fulfill({
      status: 409,
      contentType: 'application/problem+json',
      body: JSON.stringify({
        type: 'class-full',
        title: 'This class is full.',
        status: 409,
      }),
    }),
  );

  await page.goto('/classes');
  await page.getByRole('button', { name: 'Book', exact: true }).click();

  await expect(page.getByRole('alert')).toHaveText('This class is full.');
});

test('after the cut-off, cancelling shows as closed', async ({ page }) => {
  await mockApi(page, {
    signedIn: true,
    schedule: () => [{ ...spinning, bookedByMe: true }],
  });
  await page.clock.setFixedTime(new Date('2026-11-24T10:00:00Z'));

  await page.goto('/classes');

  await expect(
    page.getByText('Cancelling closed at Tue 24 Nov, 06:00.'),
  ).toBeVisible();
  await expect(
    page.getByRole('button', { name: 'Cancel booking' }),
  ).toHaveCount(0);
});
