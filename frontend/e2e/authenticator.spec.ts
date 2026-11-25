import { expect, type Page, test } from '@playwright/test';

// The authenticator app (D-059): the login code step and the setup page, against the
// built frontend with the API mocked at the network level. Synthetic data only.

const staff = {
  accountId: '0199a3c0-0000-7000-8000-0000000000bb',
  email: 'test.staff@example.com',
  firstName: 'Test',
  lastName: 'Staff',
  hasGoogleLogin: false,
  staffRoles: ['FrontDesk'],
  twoFactorEnabled: false,
  mustSetUpAuthenticator: true,
};

async function mockAccountPage(page: Page) {
  await page.route('**/api/identity/login-options', (route) =>
    route.fulfill({ json: { google: false } }),
  );
  await page.route('**/api/simulation', (route) =>
    route.fulfill({ json: { enabled: false } }),
  );
  await page.route('**/api/access/cards/mine', (route) =>
    route.fulfill({ status: 404, json: {} }),
  );
  await page.route('**/api/members/me', (route) =>
    route.fulfill({ status: 404, json: {} }),
  );
  await page.route('**/api/members/me/payments', (route) =>
    route.fulfill({ status: 404, json: {} }),
  );
  await page.route('**/api/booking/my-bookings', (route) =>
    route.fulfill({ json: [] }),
  );
}

test('an account with the authenticator on asks for the code after the password', async ({
  page,
}) => {
  await mockAccountPage(page);
  let codeSent: unknown;
  await page.route('**/api/identity/login', (route) =>
    route.fulfill({
      status: 403,
      json: { type: 'two-factor-required', status: 403 },
    }),
  );
  await page.route('**/api/identity/login/two-factor', (route) => {
    codeSent = route.request().postDataJSON();
    return route.fulfill({ status: 204 });
  });
  await page.route('**/api/identity/me', (route) =>
    route.fulfill({
      json: { ...staff, twoFactorEnabled: true, mustSetUpAuthenticator: false },
    }),
  );

  await page.goto('/login');
  await page.getByLabel('Email').fill(staff.email);
  await page.getByLabel('Password').fill('synthetic-password');
  await page.getByRole('button', { name: 'Log in' }).click();
  await page.getByLabel('Code from the app').fill('123456');
  await page.getByRole('button', { name: 'Continue' }).click();

  await expect(
    page.getByRole('heading', { name: 'Welcome, Test' }),
  ).toBeVisible();
  expect(codeSent).toEqual({ code: '123456', isRecoveryCode: false });
  await expect(page.getByText('Authenticator app: on.')).toBeVisible();
});

test('staff without the authenticator are sent to set it up, then see recovery codes', async ({
  page,
}) => {
  await mockAccountPage(page);
  await page.route('**/api/identity/me', (route) =>
    route.fulfill({ json: staff }),
  );
  await page.route('**/api/identity/authenticator', (route) =>
    route.request().method() === 'GET'
      ? route.fulfill({
          json: {
            enabled: false,
            sharedKey: 'jbsw y3dp ehpk 3pxp',
            authenticatorUri:
              'otpauth://totp/FitnessCenter:test.staff%40example.com?secret=JBSWY3DPEHPK3PXP&issuer=FitnessCenter&digits=6',
          },
        })
      : route.fulfill({ json: { codes: ['AAAAA-11111', 'BBBBB-22222'] } }),
  );

  await page.goto('/account');

  await expect(
    page.getByRole('heading', { name: 'Authenticator app' }),
  ).toBeVisible();
  await expect(
    page.getByAltText('QR code for the authenticator app'),
  ).toBeVisible();
  await expect(page.getByText('jbsw y3dp ehpk 3pxp')).toBeVisible();
  await page.getByLabel('Code from the app').fill('123456');
  await page.getByRole('button', { name: 'Turn on' }).click();

  await expect(
    page.getByRole('list', { name: 'Recovery codes' }),
  ).toContainText('AAAAA-11111');
});
