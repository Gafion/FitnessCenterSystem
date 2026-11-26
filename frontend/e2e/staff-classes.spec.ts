import { expect, type Page, test } from '@playwright/test';

// The class planning screens for managers and admins (D-062, D-068), against the built
// frontend with the API mocked at the network level. Synthetic data only. The page's
// clock is fixed so the mocked classes are in the future.

const manager = {
  accountId: '0199a3c0-0000-7000-8000-0000000000cc',
  email: 'test.manager@example.com',
  firstName: 'Test',
  lastName: 'Manager',
  hasGoogleLogin: false,
  twoFactorEnabled: true,
  mustSetUpAuthenticator: false,
  preferredLanguage: 'en',
  staffRoles: ['Manager'],
};

const spinning = {
  id: '0199a3c0-0000-7000-8000-0000000000a1',
  name: 'Spinning',
};
const yoga = { id: '0199a3c0-0000-7000-8000-0000000000a2', name: 'Yoga' };
const ida = {
  id: '0199a3c0-0000-7000-8000-0000000000b1',
  displayName: 'Ida I.',
};
const bo = { id: '0199a3c0-0000-7000-8000-0000000000b2', displayName: 'Bo B.' };

// 17:00Z on 1 Dec is 18:00 in Copenhagen (winter time, UTC+1).
const bookedClass = {
  classId: '0199a3c0-0000-7000-8000-0000000000c1',
  classTypeId: spinning.id,
  className: 'Spinning',
  instructorAccountId: ida.id,
  instructorName: 'Ida I.',
  startsAt: '2026-12-01T17:00:00Z',
  endsAt: '2026-12-01T17:45:00Z',
  lengthMinutes: 45,
  capacity: 12,
  bookedCount: 3,
  waitlistCount: 0,
  status: 'Scheduled',
  cancelledAt: null as string | null,
};

async function mockMe(page: Page, me: object) {
  await page.route('**/api/identity/me', (route) =>
    route.fulfill({ json: me }),
  );
}

/** Mocks the staff API with a list the requests change, and records what was sent. */
async function mockStaffApi(page: Page) {
  const sent: { method: string; path: string; body: unknown }[] = [];
  let classes = [bookedClass];
  await page.clock.setFixedTime(new Date('2026-11-30T12:00:00Z'));
  await mockMe(page, manager);
  await page.route('**/api/identity/staff?role=Instructor', (route) =>
    route.fulfill({ json: [ida, bo] }),
  );
  await page.route('**/api/booking/staff/class-types', (route) =>
    route.fulfill({ json: [spinning, yoga] }),
  );
  await page.route('**/api/booking/staff/classes**', (route) => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    if (request.method() === 'GET') {
      return route.fulfill({ json: classes });
    }
    sent.push({
      method: request.method(),
      path,
      body: request.postDataJSON(),
    });
    if (path.endsWith('/cancel')) {
      classes = classes.map((entry) =>
        path.includes(entry.classId)
          ? {
              ...entry,
              status: 'Cancelled',
              cancelledAt: '2026-11-30T12:00:00Z',
            }
          : entry,
      );
      return route.fulfill({ status: 204 });
    }
    return route.fulfill({ status: 200, json: bookedClass });
  });
  return sent;
}

test('a manager opens the class planning page from the account page', async ({
  page,
}) => {
  await mockStaffApi(page);
  await page.route('**/api/**', (route, request) =>
    request.url().includes('/api/identity/me') ||
    request.url().includes('/staff')
      ? route.fallback()
      : route.fulfill({ status: 404 }),
  );

  await page.goto('/account');
  await page.getByRole('link', { name: 'Plan classes' }).click();

  await expect(
    page.getByRole('heading', { name: 'Classes', level: 1 }),
  ).toBeVisible();
  const item = page
    .getByRole('region', { name: 'Upcoming classes' })
    .getByRole('listitem', { name: 'Spinning' });
  await expect(item).toContainText('18:00–18:45');
  await expect(item).toContainText('Ida I.');
  await expect(item).toContainText('3 of 12 spots booked');
});

test('a manager schedules a class in Danish time', async ({ page }) => {
  const sent = await mockStaffApi(page);
  await page.goto('/staff/classes');

  await page.getByRole('button', { name: 'Schedule a class' }).click();
  const form = page.getByRole('form', { name: 'Schedule a class' });
  await form.getByRole('combobox', { name: 'Class type' }).click();
  await page.getByRole('option', { name: 'Yoga' }).click();
  await form.getByRole('combobox', { name: 'Instructor' }).click();
  // Sorted by name: Bo before Ida.
  await expect(page.getByRole('option')).toHaveText(['Bo B.', 'Ida I.']);
  await page.getByRole('option', { name: 'Bo B.' }).click();
  await form.getByLabel('Date').fill('2026-12-03');
  await form.getByLabel('Time').fill('07:30');
  await form.getByLabel('Length in minutes').fill('60');
  await form.getByLabel('Spots').fill('10');
  await form.getByRole('button', { name: 'Schedule class' }).click();

  await expect(form).toHaveCount(0);
  expect(sent).toEqual([
    {
      method: 'POST',
      path: '/api/booking/staff/classes',
      body: {
        classTypeId: yoga.id,
        instructorAccountId: bo.id,
        startsAt: '2026-12-03T06:30:00.000Z',
        lengthMinutes: 60,
        capacity: 10,
      },
    },
  ]);
});

test('moving a booked class says the members will be emailed', async ({
  page,
}) => {
  const sent = await mockStaffApi(page);
  await page.goto('/staff/classes');

  const item = page
    .getByRole('region', { name: 'Upcoming classes' })
    .getByRole('listitem', { name: 'Spinning' });
  await item.getByRole('button', { name: 'Edit' }).click();
  const form = item.getByRole('form', { name: 'Edit Spinning' });
  await expect(form.getByLabel('Spots')).toHaveAttribute('min', '3');
  await form.getByLabel('Time').fill('19:00');
  await expect(form.getByRole('status')).toHaveText(
    'The 3 booked members will be emailed the new time and may then cancel until the class starts.',
  );
  await form.getByRole('button', { name: 'Save changes' }).click();

  await expect(form).toHaveCount(0);
  expect(sent).toEqual([
    {
      method: 'PUT',
      path: `/api/booking/staff/classes/${bookedClass.classId}`,
      body: {
        classTypeId: spinning.id,
        instructorAccountId: ida.id,
        startsAt: '2026-12-01T18:00:00.000Z',
        lengthMinutes: 45,
        capacity: 12,
      },
    },
  ]);
});

test('cancelling a class asks first and then shows it as cancelled', async ({
  page,
}) => {
  const sent = await mockStaffApi(page);
  await page.goto('/staff/classes');

  const item = page
    .getByRole('region', { name: 'Upcoming classes' })
    .getByRole('listitem', { name: 'Spinning' });
  await item.getByRole('button', { name: 'Cancel class' }).click();
  await expect(item).toContainText(
    'The 3 booked members get an email that the class is cancelled, and their bookings are cancelled.',
  );
  expect(sent).toEqual([]);
  await item.getByRole('button', { name: 'Yes, cancel the class' }).click();

  await expect(item).toContainText('Cancelled');
  await expect(item.getByRole('button')).toHaveCount(0);
  expect(sent.map((request) => request.path)).toEqual([
    `/api/booking/staff/classes/${bookedClass.classId}/cancel`,
  ]);
});

test('someone without the manager policy is told the page is for managers', async ({
  page,
}) => {
  await mockMe(page, { ...manager, staffRoles: ['Instructor'] });
  await page.route('**/api/booking/staff/**', (route) =>
    route.fulfill({ status: 403 }),
  );
  await page.route('**/api/identity/staff?role=Instructor', (route) =>
    route.fulfill({ status: 403 }),
  );

  await page.goto('/staff/classes');

  await expect(
    page.getByText('Only managers and admins plan the classes.'),
  ).toBeVisible();
});

test('a logged-out visitor is sent to the login page', async ({ page }) => {
  await page.route('**/api/identity/me', (route) =>
    route.fulfill({ status: 401 }),
  );

  await page.goto('/staff/classes');

  await expect(page).toHaveURL(/\/login$/);
});
