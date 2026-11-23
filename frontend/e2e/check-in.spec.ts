import { expect, type Page, test } from '@playwright/test';

// The simulated card reader against the built frontend with the API mocked at the
// network level, like classes.spec.ts. Check-in itself is covered by the backend's
// integration tests (AccessTests). Synthetic data only.

const account = {
  accountId: '0199a3c0-0000-7000-8000-0000000000aa',
  email: 'test.person@example.com',
  firstName: 'Test',
  lastName: 'Person',
  hasGoogleLogin: false,
};

const myCard = 'SIM-04A1B2C3D4E5F6';
const at = '2027-01-12T06:30:00Z';

async function mockApi(page: Page, options: { simulation: boolean }) {
  await page.route('**/api/identity/me', (route) =>
    route.fulfill({ json: account }),
  );
  await page.route('**/api/identity/login-options', (route) =>
    route.fulfill({ json: { google: false } }),
  );
  await page.route('**/api/access/cards/mine', (route) =>
    route.fulfill({ json: { uid: myCard } }),
  );
  await page.route('**/api/access/simulation/reader', (route) =>
    route.fulfill({ status: options.simulation ? 204 : 404 }),
  );
  await page.route('**/api/simulation', (route) =>
    route.fulfill({ json: { enabled: options.simulation } }),
  );
}

test('a member opens the reader from the account page and swipes their card', async ({
  page,
}) => {
  await mockApi(page, { simulation: true });
  await page.route('**/api/access/simulation/reader/swipes/my-card', (route) =>
    route.fulfill({ json: { outcome: 'welcome', firstName: 'Test', at } }),
  );

  await page.goto('/account');
  await expect(page.getByText(myCard)).toBeVisible();
  await page.getByRole('link', { name: 'Try the card reader' }).click();

  await expect(
    page.getByRole('complementary', { name: 'Simulation' }),
  ).toBeVisible();
  await expect(
    page.getByRole('heading', { name: 'Card reader' }),
  ).toBeVisible();
  await page.getByRole('button', { name: 'Swipe my card' }).click();

  await expect(page.getByText('Welcome, Test!')).toBeVisible();
});

test('a typed UID nobody has is an unknown card', async ({ page }) => {
  let sent: unknown;
  await mockApi(page, { simulation: true });
  await page.route('**/api/access/simulation/reader/swipes', (route) => {
    sent = route.request().postDataJSON();
    return route.fulfill({
      json: { outcome: 'unknown-card', firstName: null, at },
    });
  });

  await page.goto('/simulation/check-in');
  await page.getByLabel('Card UID').fill('SIM-00000000000000');
  await page.getByRole('button', { name: 'Swipe', exact: true }).click();

  await expect(page.getByText('Unknown card')).toBeVisible();
  expect(sent).toEqual({ cardUid: 'SIM-00000000000000' });
});

test('with the simulation off, the page and the link are gone', async ({
  page,
}) => {
  await mockApi(page, { simulation: false });

  await page.goto('/account');
  await expect(page.getByText(myCard)).toBeVisible();
  await expect(
    page.getByRole('link', { name: 'Try the card reader' }),
  ).toHaveCount(0);

  await page.goto('/simulation/check-in');
  await expect(page.getByRole('heading', { name: 'Card reader' })).toHaveCount(
    0,
  );
  await expect(page.getByText('Not Found')).toBeVisible();
});
