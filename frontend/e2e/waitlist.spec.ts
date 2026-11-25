import { expect, test } from '@playwright/test';

// The waitlist buttons on the classes page (D-051, FIT-79), against the built
// frontend with the API mocked at the network level, like classes.spec.ts. The
// queue rules themselves are covered by the backend's tests (WaitlistTests).

test.use({ timezoneId: 'America/New_York' });

const now = new Date('2026-11-23T12:00:00Z');

const fullSpinning = {
  classId: '0199a3c0-0000-7000-8000-000000000c01',
  className: 'Spinning',
  instructorName: 'Test Instructor',
  startsAt: '2026-11-24T17:00:00Z',
  endsAt: '2026-11-24T17:45:00Z',
  capacity: 12,
  spotsLeft: 0,
  bookedByMe: false,
  cancelUntil: '2026-11-24T05:00:00Z',
  afterMyMembershipEnds: false,
  myWaitlistPlace: null as number | null,
};

test('a member joins the waitlist of a full class and leaves it', async ({
  page,
}) => {
  let place: number | null = null;
  const calls: string[] = [];
  await page.clock.setFixedTime(now);
  await page.route('**/api/booking/schedule', (route) =>
    route.fulfill({ json: [{ ...fullSpinning, myWaitlistPlace: place }] }),
  );
  await page.route('**/api/identity/me', (route) =>
    route.fulfill({
      json: {
        accountId: '0199a3c0-0000-7000-8000-0000000000aa',
        email: 'test.person@example.com',
        firstName: 'Test',
        lastName: 'Person',
        hasGoogleLogin: false,
      },
    }),
  );
  await page.route(
    `**/api/booking/classes/${fullSpinning.classId}/waitlist`,
    (route) => {
      calls.push(route.request().method());
      place = route.request().method() === 'POST' ? 2 : null;
      return route.fulfill({ status: 204 });
    },
  );

  await page.goto('/classes');
  await expect(page.getByText('This class is full.')).toBeVisible();
  await page.getByRole('button', { name: 'Join waitlist' }).click();

  await expect(page.getByText("You're on the waitlist.")).toBeVisible();
  await expect(page.getByText("You're number 2 in the queue.")).toBeVisible();

  await page.getByRole('button', { name: 'Leave waitlist' }).click();

  await expect(
    page.getByRole('button', { name: 'Join waitlist' }),
  ).toBeVisible();
  expect(calls).toEqual(['POST', 'DELETE']);
});

test('a visitor who is not logged in is sent to login to join', async ({
  page,
}) => {
  let posted = false;
  await page.clock.setFixedTime(now);
  await page.route('**/api/booking/schedule', (route) =>
    route.fulfill({ json: [fullSpinning] }),
  );
  await page.route('**/api/identity/me', (route) =>
    route.fulfill({ status: 401 }),
  );
  await page.route('**/api/identity/login-options', (route) =>
    route.fulfill({ json: { google: false } }),
  );
  await page.route('**/api/booking/classes/*/waitlist', async (route) => {
    posted = true;
    await route.fulfill({ status: 401 });
  });

  await page.goto('/classes');
  await page.getByRole('button', { name: 'Join waitlist' }).click();

  await expect(page.getByRole('heading', { name: 'Log in' })).toBeVisible();
  expect(posted).toBe(false);
});
