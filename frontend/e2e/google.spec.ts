import { expect, test } from '@playwright/test';

// Google itself can't run in CI; these check the pages around it with the API mocked.
// The API side is covered by GoogleLoginTests (integration tests with a fake Google).

const terms = {
  membershipName: 'General membership',
  monthlyPriceOre: 29_900,
  termsVersion: 'terms-v1',
  termsText: 'Synthetic terms text.',
  earlyStartVersion: 'early-v1',
  earlyStartText: 'Synthetic early-start text.',
};

test('login offers Google when the API has it configured', async ({ page }) => {
  await page.route('**/api/identity/login-options', (route) =>
    route.fulfill({ json: { google: true } }),
  );

  await page.goto('/login');

  await expect(
    page.getByRole('link', { name: 'Log in with Google' }),
  ).toHaveAttribute('href', '/api/identity/external/google');
});

test('login hides Google when the API has no Google client', async ({
  page,
}) => {
  await page.route('**/api/identity/login-options', (route) =>
    route.fulfill({ json: { google: false } }),
  );

  await page.goto('/login');

  await expect(page.getByRole('heading', { name: 'Log in' })).toBeVisible();
  await expect(
    page.getByRole('link', { name: 'Log in with Google' }),
  ).toHaveCount(0);
});

test('a Google email that already has an account explains what to do', async ({
  page,
}) => {
  await page.goto('/login?error=google-email-taken');

  await expect(
    page.getByText(
      'This email already has an account. Log in with your password, then connect Google from your account page.',
    ),
  ).toBeVisible();
});

test('a new Google user accepts the terms and lands on their account', async ({
  page,
}) => {
  let signedIn = false;
  let sent: unknown;
  await page.route('**/api/identity/external/pending', (route) =>
    route.fulfill({
      json: {
        email: 'test.googler@example.com',
        firstName: 'Test',
        lastName: 'Googler',
      },
    }),
  );
  await page.route('**/api/members/signup/terms', (route) =>
    route.fulfill({ json: terms }),
  );
  await page.route('**/api/members/signup/google', async (route) => {
    sent = route.request().postDataJSON();
    signedIn = true;
    await route.fulfill({
      status: 201,
      json: { memberId: '0199a3c0-0000-7000-8000-0000000000cc' },
    });
  });
  await page.route('**/api/identity/me', (route) =>
    signedIn
      ? route.fulfill({
          json: {
            accountId: '0199a3c0-0000-7000-8000-0000000000dd',
            email: 'test.googler@example.com',
            firstName: 'Test',
            lastName: 'Googler',
            hasGoogleLogin: true,
          },
        })
      : route.fulfill({ status: 401 }),
  );

  await page.goto('/signup/google');
  await expect(page.getByLabel('First name')).toHaveValue('Test');
  await page.getByLabel('I accept the terms').check();
  await page.getByRole('button', { name: 'Sign up' }).click();

  await expect(
    page.getByRole('heading', { name: 'Welcome, Test' }),
  ).toBeVisible();
  await expect(
    page.getByText('Google is connected. You can log in with it.'),
  ).toBeVisible();
  expect(sent).toEqual({
    firstName: 'Test',
    lastName: 'Googler',
    acceptTerms: true,
    termsVersion: 'terms-v1',
    startNow: false,
    earlyStartVersion: 'early-v1',
  });
});

test('finishing a Google signup without a Google login goes back to login', async ({
  page,
}) => {
  await page.route('**/api/identity/external/pending', (route) =>
    route.fulfill({ status: 401 }),
  );

  await page.goto('/signup/google');

  await expect(
    page.getByText("The Google login didn't work. Please try again."),
  ).toBeVisible();
});

test('a member without Google can connect it from the account page', async ({
  page,
}) => {
  await page.route('**/api/identity/login-options', (route) =>
    route.fulfill({ json: { google: true } }),
  );
  await page.route('**/api/identity/me', (route) =>
    route.fulfill({
      json: {
        accountId: '0199a3c0-0000-7000-8000-0000000000ee',
        email: 'test.person@example.com',
        firstName: 'Test',
        lastName: 'Person',
        hasGoogleLogin: false,
      },
    }),
  );

  await page.goto('/account');

  await expect(
    page.getByRole('link', { name: 'Connect Google' }),
  ).toHaveAttribute('href', '/api/identity/external/google/link');
});
