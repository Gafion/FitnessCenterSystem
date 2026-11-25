import { expect, test } from '@playwright/test';
import { linkPathFromEmail, syntheticMember } from './member';
import { devAdmin, logInAsStaff } from './staff';
import { totp } from './totp';

// The staff invite (D-058), end to end: the seeded admin logs in with the
// authenticator (FIT-109) and invites a new instructor; the invite email is
// read from Mailpit; the new instructor accepts it, logs in and must set up
// the authenticator before using staff pages (D-059). Synthetic data only.

test('an admin invites an instructor, who accepts and sets up the authenticator', async ({
  page,
  request,
}) => {
  const instructor = { ...syntheticMember(), firstName: 'Invited' };

  await logInAsStaff(page, devAdmin);
  await page.goto('/staff/invite');
  await expect(
    page.getByRole('heading', { name: 'Invite staff' }),
  ).toBeVisible();
  await page.getByRole('textbox', { name: 'Email' }).fill(instructor.email);
  await page.getByRole('radio', { name: 'Instructor' }).click();
  await page
    .getByRole('radiogroup', { name: 'Language of the email' })
    .getByRole('radio', { name: 'English' })
    .click();
  await page.getByRole('button', { name: 'Send invite' }).click();
  await expect(
    page.getByText(`Invite sent to ${instructor.email} as Instructor.`),
  ).toBeVisible();

  // The invitee opens the link in a browser with no one logged in.
  await page.goto('/account');
  await page.getByRole('button', { name: 'Log out' }).click();
  await expect(page).toHaveURL(/\/$/);
  const invitee = page;

  await invitee.goto(
    await linkPathFromEmail(request, instructor.email, '/accept-invite'),
  );
  await expect(
    invitee.getByRole('heading', { name: 'Set up your staff account' }),
  ).toBeVisible();
  await invitee.getByLabel('First name').fill(instructor.firstName);
  await invitee.getByLabel('Last name').fill('Instructor');
  await invitee.getByLabel(/^Password/).fill(instructor.password);
  await invitee.getByRole('button', { name: 'Create staff account' }).click();
  await expect(
    invitee.getByRole('heading', { name: 'Your staff account is ready' }),
  ).toBeVisible();

  // The password alone lets a new staff member in only to set up the app.
  await invitee.goto('/login');
  await invitee.getByLabel('Email').fill(instructor.email);
  await invitee.getByLabel('Password').fill(instructor.password);
  await invitee.getByRole('button', { name: 'Log in', exact: true }).click();
  await expect(invitee).toHaveURL(/\/authenticator$/);
  await expect(
    invitee.getByText(
      'Staff accounts need an authenticator app before they can use staff pages.',
    ),
  ).toBeVisible();

  // The setup key, as typed into an app by hand.
  const setupKey = invitee.locator('code');
  await expect(setupKey).toBeVisible();
  const key = (await setupKey.textContent()) ?? '';
  await invitee.getByLabel('Code from the app').fill(totp(key));
  await invitee.getByRole('button', { name: 'Turn on' }).click();
  await expect(
    invitee.getByRole('list', { name: 'Recovery codes' }),
  ).toBeVisible();
  await invitee.getByRole('button', { name: "I've saved them" }).click();
  await expect(
    invitee.getByRole('heading', {
      name: `Welcome, ${instructor.firstName}`,
    }),
  ).toBeVisible();
  await expect(invitee.getByText('Authenticator app: on.')).toBeVisible();
});
