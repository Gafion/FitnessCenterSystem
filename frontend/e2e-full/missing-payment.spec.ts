import { expect, test } from '@playwright/test';
import { mailpitUrl, signUpAndLogIn } from './member';

// Full stack (D-043): a member who paid only for a past month has a missing payment
// (D-020, D-042). The simulated reader refuses them, and the reminder job, which runs
// every 30 seconds in Development, sends them one reminder email.
test('a member who paid only for a past month is refused at the reader and reminded', async ({
  page,
  request,
}) => {
  // The reminder can take up to one job interval (30 seconds) to arrive.
  test.setTimeout(90_000);
  const member = await signUpAndLogIn(page, request);

  await page.getByRole('link', { name: 'Simulated payments' }).click();
  await page.getByLabel('Or pay one month starting on').fill('2020-01-01');
  await page.getByRole('button', { name: 'Pay for this month' }).click();
  await expect(page.getByText(/Paid until/)).toBeVisible();

  await page.goto('/simulation/check-in');
  await page.getByRole('button', { name: 'Swipe my card' }).click();

  await expect(page.getByRole('status')).toContainText('Payment missing');
  await expect(page.getByText(`Welcome, ${member.firstName}!`)).toHaveCount(0);

  await expect
    .poll(
      async () => {
        const search = await request.get(`${mailpitUrl()}/api/v1/search`, {
          params: {
            query: `to:"${member.email}" subject:"Your payment is missing"`,
          },
        });
        return ((await search.json()) as { messages: unknown[] }).messages
          .length;
      },
      { message: 'one reminder email', timeout: 60_000 },
    )
    .toBe(1);
});
