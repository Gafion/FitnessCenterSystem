import { expect, test } from '@playwright/test';
import { confirmEmail, logIn, signUp, syntheticMember } from './member';

// Full stack (D-043): the real API sends the confirmation email to Mailpit, and
// the test follows the link from it.
test('a new member signs up, confirms their email and logs in', async ({
  page,
  request,
}) => {
  const member = syntheticMember();

  await signUp(page, member);
  await confirmEmail(page, request, member);
  await logIn(page, member);

  await expect(page.getByText(member.email)).toBeVisible();
});
