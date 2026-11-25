import { expect, test } from '@playwright/test';
import {
  devAdmin,
  enterCode,
  enterStaffPassword,
  logInAsStaff,
  staffCode,
} from './staff';

// A staff login with the authenticator app (D-013, D-059), end to end: the
// password, then a code computed from the seeded key (FIT-109).

test('staff log in with a code from the authenticator app', async ({
  page,
}) => {
  await logInAsStaff(page, devAdmin);

  await expect(page.getByText('Authenticator app: on.')).toBeVisible();
});

test('an old code keeps staff out', async ({ page }) => {
  await enterStaffPassword(page, devAdmin);
  // Ten minutes old: far outside the window the API accepts.
  await enterCode(page, staffCode(new Date(Date.now() - 10 * 60_000)));

  await expect(page.getByText("That code didn't work.")).toBeVisible();
  await expect(
    page.getByRole('heading', { name: `Welcome, ${devAdmin.firstName}` }),
  ).toBeHidden();
});
