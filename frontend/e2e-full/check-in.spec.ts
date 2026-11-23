import { expect, test } from '@playwright/test';
import { signUpAndLogIn } from './member';

// Full stack (D-043): a new member gets a simulated card (D-021), and the simulated
// reader, which is on in Development (Simulation:Enabled), lets them in.
test('a new member swipes their card at the simulated reader', async ({
  page,
  request,
}) => {
  const member = await signUpAndLogIn(page, request);

  await page.getByRole('link', { name: 'Try the card reader' }).click();
  await expect(
    page.getByRole('heading', { name: 'Card reader' }),
  ).toBeVisible();
  await page.getByRole('button', { name: 'Swipe my card' }).click();

  await expect(page.getByText(`Welcome, ${member.firstName}!`)).toBeVisible();
});
