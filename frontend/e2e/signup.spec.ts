import { expect, type Page, test } from '@playwright/test';

// These run against the built frontend with the API mocked at the network level;
// the backend's own behaviour is covered by its integration tests.

const terms = {
  membershipName: 'General membership',
  monthlyPriceOre: 29_900,
  termsVersion: 'terms-v1',
  termsText: 'Synthetic terms text.',
  earlyStartVersion: 'early-v1',
  earlyStartText: 'Synthetic early-start text.',
};

const account = {
  accountId: '0199a3c0-0000-7000-8000-0000000000aa',
  email: 'test.person@example.com',
  firstName: 'Test',
  lastName: 'Person',
};

async function mockApi(page: Page, options: { signedIn: () => boolean }) {
  await page.route('**/api/members/signup/terms', (route) =>
    route.fulfill({ json: terms }),
  );
  await page.route('**/api/identity/me', (route) =>
    options.signedIn()
      ? route.fulfill({ json: account })
      : route.fulfill({ status: 401 }),
  );
}

test('a new member signs up and lands on their account page', async ({
  page,
}) => {
  let signedIn = false;
  let sent: unknown;
  await mockApi(page, { signedIn: () => signedIn });
  await page.route('**/api/members/signup', async (route) => {
    sent = route.request().postDataJSON();
    signedIn = true;
    await route.fulfill({
      status: 201,
      json: { memberId: '0199a3c0-0000-7000-8000-0000000000bb' },
    });
  });

  await page.goto('/');
  await page.getByRole('link', { name: 'Sign up' }).click();
  await page.getByLabel('First name').fill('Test');
  await page.getByLabel('Last name').fill('Person');
  await page.getByLabel('Email').fill('test.person@example.com');
  await page.getByLabel(/^Password/).fill('correct-horse');
  await page.getByLabel('I accept the terms').check();
  await page.getByRole('button', { name: 'Sign up' }).click();

  await expect(
    page.getByRole('heading', { name: 'Welcome, Test' }),
  ).toBeVisible();
  expect(sent).toMatchObject({
    acceptTerms: true,
    termsVersion: 'terms-v1',
    startNow: false,
  });
});

test('signup errors from the API are shown on the form', async ({ page }) => {
  await mockApi(page, { signedIn: () => false });
  await page.route('**/api/members/signup', (route) =>
    route.fulfill({
      status: 400,
      contentType: 'application/problem+json',
      body: JSON.stringify({
        title: 'One or more validation errors occurred.',
        status: 400,
        errors: {
          email: ["Email 'test.person@example.com' is already taken."],
        },
      }),
    }),
  );

  await page.goto('/signup');
  await page.getByRole('button', { name: 'Sign up' }).click();

  await expect(page.getByLabel('Email')).toHaveAccessibleDescription(
    "Email 'test.person@example.com' is already taken.",
  );
});

test('the account page sends visitors who are not logged in to login', async ({
  page,
}) => {
  await mockApi(page, { signedIn: () => false });

  await page.goto('/account');

  await expect(page.getByRole('heading', { name: 'Log in' })).toBeVisible();
});

test('a wrong login shows one message for any wrong field', async ({
  page,
}) => {
  await page.route('**/api/identity/login', (route) =>
    route.fulfill({ status: 401 }),
  );

  await page.goto('/login');
  await page.getByLabel('Email').fill('test.person@example.com');
  await page.getByLabel('Password').fill('wrong-password');
  await page.getByRole('button', { name: 'Log in' }).click();

  await expect(page.getByText('The email or password is wrong.')).toBeVisible();
});
