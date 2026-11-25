import { expect, type Page, test } from '@playwright/test';

// The header shows "My account" only when logged in, and "Log in" and
// "Become a member" only when logged out, on desktop and in the phone menu
// (FIT-103). The API is mocked at the network level. Synthetic data only.

const account = {
  accountId: '0199a3c0-0000-7000-8000-0000000000ee',
  email: 'test.member@example.com',
  firstName: 'Test',
  lastName: 'Member',
  hasGoogleLogin: false,
  twoFactorEnabled: false,
  mustSetUpAuthenticator: false,
  preferredLanguage: 'en',
};

async function session(page: Page, loggedIn: boolean) {
  await page.route('**/api/identity/me', (route) =>
    loggedIn
      ? route.fulfill({ json: account })
      : route.fulfill({ status: 401 }),
  );
}

test('logged in, the header shows My account only', async ({ page }) => {
  await session(page, true);
  await page.goto('/');
  const header = page.getByRole('banner');

  await expect(header.getByRole('link', { name: 'My account' })).toBeVisible();
  await expect(header.getByRole('link', { name: 'Log in' })).toHaveCount(0);
  await expect(
    header.getByRole('link', { name: 'Become a member' }),
  ).toHaveCount(0);
});

test('logged out, the header shows Log in and Become a member only', async ({
  page,
}) => {
  await session(page, false);
  await page.goto('/');
  const header = page.getByRole('banner');

  await expect(header.getByRole('link', { name: 'Log in' })).toBeVisible();
  await expect(
    header.getByRole('link', { name: 'Become a member' }),
  ).toBeVisible();
  await expect(header.getByRole('link', { name: 'My account' })).toHaveCount(0);
});

test('the phone menu follows the same rule when logged in', async ({
  page,
}) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await session(page, true);
  await page.goto('/');

  await page.getByRole('button', { name: 'Menu' }).click();
  const menu = page.getByRole('menu');

  await expect(
    menu.getByRole('menuitem', { name: 'My account' }),
  ).toBeVisible();
  await expect(menu.getByRole('menuitem', { name: 'Log in' })).toHaveCount(0);
  await expect(
    menu.getByRole('menuitem', { name: 'Become a member' }),
  ).toHaveCount(0);
});

test('the phone menu follows the same rule when logged out', async ({
  page,
}) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await session(page, false);
  await page.goto('/');

  await page.getByRole('button', { name: 'Menu' }).click();
  const menu = page.getByRole('menu');

  await expect(menu.getByRole('menuitem', { name: 'Log in' })).toBeVisible();
  await expect(
    menu.getByRole('menuitem', { name: 'Become a member' }),
  ).toBeVisible();
  await expect(menu.getByRole('menuitem', { name: 'My account' })).toHaveCount(
    0,
  );
});
