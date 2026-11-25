import { expect, type Page, test } from '@playwright/test';

// A spot held for the member from the waitlist (D-051, D-067, FIT-80), on the classes
// page and the account page, against the built frontend with the API mocked at the
// network level like waitlist.spec.ts. The offer rules themselves are covered by the
// backend's tests (WaitlistOfferTests). Synthetic data only.

test.use({ timezoneId: 'America/New_York' });

const now = new Date('2026-11-23T12:00:00Z');

/** 14:00 in Copenhagen on Monday 23 November 2026. */
const expiresAt = '2026-11-23T13:00:00Z';

const heldSpinning = {
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
  myWaitlistPlace: null,
  myOfferExpiresAt: expiresAt as string | null,
};

const offerUrl = `**/api/booking/classes/${heldSpinning.classId}/offer`;

async function mockMe(page: Page) {
  await page.route('**/api/identity/me', (route) =>
    route.fulfill({
      json: {
        accountId: '0199a3c0-0000-7000-8000-0000000000aa',
        email: 'test.person@example.com',
        firstName: 'Test',
        lastName: 'Person',
        hasGoogleLogin: false,
        staffRoles: [],
      },
    }),
  );
}

test('a member accepts a held spot on the classes page', async ({ page }) => {
  let booked = false;
  const calls: string[] = [];
  await page.clock.setFixedTime(now);
  await mockMe(page);
  await page.route('**/api/booking/schedule', (route) =>
    route.fulfill({
      json: [
        booked
          ? { ...heldSpinning, bookedByMe: true, myOfferExpiresAt: null }
          : heldSpinning,
      ],
    }),
  );
  await page.route(offerUrl, (route) => {
    calls.push(route.request().method());
    booked = true;
    return route.fulfill({ status: 204 });
  });

  await page.goto('/classes');
  await expect(page.getByText('A spot is held for you.')).toBeVisible();
  await expect(
    page.getByText(/Accept by .*14:00, or it goes to the next person\./),
  ).toBeVisible();
  await expect(
    page.getByRole('button', { name: 'Join waitlist' }),
  ).not.toBeVisible();
  await page.getByRole('button', { name: 'Accept spot' }).click();

  await expect(page.getByText("You're booked.")).toBeVisible();
  expect(calls).toEqual(['POST']);
});

test('an offer that ran out says so on the classes page', async ({ page }) => {
  let expired = false;
  await page.clock.setFixedTime(now);
  await mockMe(page);
  await page.route('**/api/booking/schedule', (route) =>
    route.fulfill({
      json: [
        expired ? { ...heldSpinning, myOfferExpiresAt: null } : heldSpinning,
      ],
    }),
  );
  await page.route(offerUrl, (route) => {
    expired = true;
    return route.fulfill({
      status: 409,
      contentType: 'application/problem+json',
      json: { type: 'offer-expired', status: 409 },
    });
  });

  await page.goto('/classes');
  await page.getByRole('button', { name: 'Accept spot' }).click();

  await expect(page.getByRole('alert')).toHaveText(
    'The offer has run out, so the spot went to the next person.',
  );
  await expect(page.getByText('This class is full.')).toBeVisible();
});

async function mockAccount(page: Page, offers: () => object[]) {
  await mockMe(page);
  await page.route('**/api/identity/login-options', (route) =>
    route.fulfill({ json: { google: false } }),
  );
  await page.route('**/api/simulation', (route) =>
    route.fulfill({ json: { enabled: false } }),
  );
  await page.route('**/api/access/cards/mine', (route) =>
    route.fulfill({ status: 404 }),
  );
  await page.route('**/api/members/me', (route) =>
    route.fulfill({ status: 404 }),
  );
  await page.route('**/api/members/me/payments', (route) =>
    route.fulfill({ status: 404 }),
  );
  await page.route('**/api/booking/my-bookings', (route) =>
    route.fulfill({ json: [] }),
  );
  await page.route('**/api/booking/my-offers', (route) =>
    route.fulfill({ json: offers() }),
  );
}

const offer = {
  classId: heldSpinning.classId,
  className: 'Spinning',
  instructorName: 'Test Instructor',
  startsAt: heldSpinning.startsAt,
  endsAt: heldSpinning.endsAt,
  expiresAt,
};

for (const [answer, method] of [
  ['Accept spot', 'POST'],
  ['Decline', 'DELETE'],
] as const) {
  test(`the account page shows an open offer, and ${answer} answers it`, async ({
    page,
  }) => {
    let answered = false;
    const calls: string[] = [];
    await page.clock.setFixedTime(now);
    await mockAccount(page, () => (answered ? [] : [offer]));
    await page.route(offerUrl, (route) => {
      calls.push(route.request().method());
      answered = true;
      return route.fulfill({ status: 204 });
    });

    await page.goto('/account');
    const notice = page.getByRole('region', { name: 'A spot is free for you' });
    await expect(notice).toBeVisible();
    await expect(notice).toContainText(/Spinning, .*18:00–18:45, Danish time/);
    await expect(notice).toContainText(/Accept by .*14:00/);
    await notice.getByRole('button', { name: answer }).click();

    await expect(notice).not.toBeVisible();
    expect(calls).toEqual([method]);
  });
}

test('the account page shows no notice without an offer', async ({ page }) => {
  await page.clock.setFixedTime(now);
  await mockAccount(page, () => []);

  await page.goto('/account');

  await expect(
    page.getByRole('heading', { name: 'Welcome, Test' }),
  ).toBeVisible();
  await expect(page.getByText('A spot is free for you')).not.toBeVisible();
});
