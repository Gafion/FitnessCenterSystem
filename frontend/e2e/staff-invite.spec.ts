import { expect, type Page, test } from '@playwright/test';

// The staff invite screen (D-058, D-063, D-065), against the built frontend with the API
// mocked at the network level. Synthetic data only.

const staff = {
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

async function mockMe(page: Page, me: object) {
  await page.route('**/api/identity/me', (route) =>
    route.fulfill({ json: me }),
  );
}

test('a manager invites an instructor and sees when the link expires', async ({
  page,
}) => {
  await mockMe(page, staff);
  await page.route('**/api/identity/staff-invites/roles', (route) =>
    route.fulfill({ json: { roles: ['FrontDesk', 'Instructor'] } }),
  );
  let sent: unknown;
  await page.route('**/api/identity/staff-invites', (route) => {
    sent = route.request().postDataJSON();
    return route.fulfill({
      status: 201,
      json: {
        inviteId: '0199a3c0-0000-7000-8000-0000000000dd',
        email: 'new.instructor@example.com',
        role: 'Instructor',
        expiresAt: '2026-12-02T12:00:00Z',
      },
    });
  });

  await page.goto('/staff/invite');
  await expect(
    page.getByRole('heading', { name: 'Invite staff' }),
  ).toBeVisible();
  // A manager may not give Manager or Admin (D-063): the screen doesn't offer them.
  await expect(page.getByRole('radio', { name: 'Manager' })).toHaveCount(0);
  await expect(page.getByRole('radio', { name: 'Admin' })).toHaveCount(0);

  await page
    .getByRole('textbox', { name: 'Email' })
    .fill('new.instructor@example.com');
  await page.getByRole('radio', { name: 'Instructor' }).click();
  await page
    .getByRole('radiogroup', { name: 'Language of the email' })
    .getByRole('radio', { name: 'Dansk' })
    .click();
  await page.getByRole('button', { name: 'Send invite' }).click();

  await expect(
    page.getByText(
      'Invite sent to new.instructor@example.com as Instructor. The link works until 2 Dec 2026, 13:00.',
    ),
  ).toBeVisible();
  expect(sent).toEqual({
    email: 'new.instructor@example.com',
    role: 'Instructor',
    language: 'da',
  });
});

test('the API refusing a role shows why', async ({ page }) => {
  await mockMe(page, { ...staff, staffRoles: ['Admin'] });
  await page.route('**/api/identity/staff-invites/roles', (route) =>
    route.fulfill({
      json: { roles: ['Admin', 'Manager', 'FrontDesk', 'Instructor'] },
    }),
  );
  await page.route('**/api/identity/staff-invites', (route) =>
    route.fulfill({
      status: 409,
      contentType: 'application/problem+json',
      json: { type: 'already-has-role', status: 409 },
    }),
  );

  await page.goto('/staff/invite');
  await page
    .getByRole('textbox', { name: 'Email' })
    .fill('test.member@example.com');
  await page.getByRole('radio', { name: 'Front desk' }).click();
  await page.getByRole('button', { name: 'Send invite' }).click();

  await expect(
    page.getByText('This account already has this role.'),
  ).toBeVisible();
});

test('a member is told only admins and managers invite', async ({ page }) => {
  await mockMe(page, { ...staff, staffRoles: [] });
  await page.route('**/api/identity/staff-invites/roles', (route) =>
    route.fulfill({ status: 403 }),
  );

  await page.goto('/staff/invite');

  await expect(
    page.getByText('Only admins and managers can invite staff.'),
  ).toBeVisible();
  await expect(page.getByRole('textbox', { name: 'Email' })).toHaveCount(0);
});

test('staff without the authenticator are sent to set it up first', async ({
  page,
}) => {
  await mockMe(page, {
    ...staff,
    twoFactorEnabled: false,
    mustSetUpAuthenticator: true,
  });
  await page.route('**/api/identity/authenticator', (route) =>
    route.fulfill({
      json: {
        enabled: false,
        sharedKey: 'JBSWY3DPEHPK3PXP',
        authenticatorUri:
          'otpauth://totp/FitnessCenter:test.manager@example.com?secret=JBSWY3DPEHPK3PXP',
      },
    }),
  );

  await page.goto('/staff/invite');

  await expect(page).toHaveURL(/\/authenticator$/);
});
