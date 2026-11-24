import { expect, type Locator, test } from '@playwright/test';
import { signUpAndLogIn } from './member';

// Full stack (D-043): booking against the classes the API seeds in Development
// (Booking:SeedDevelopmentSchedule). Other runs may have booked the same class, so
// the test reads the count first and checks that it moves by one.

async function spotsLeft(card: Locator): Promise<number> {
  const text = await card.getByText(/^\d+ spots? left$/).textContent();
  return Number.parseInt(text ?? '', 10);
}

function spots(count: number): string {
  return count === 1 ? '1 spot left' : `${count} spots left`;
}

test('a member books a class, sees it on the account page and cancels it', async ({
  page,
  request,
}) => {
  await signUpAndLogIn(page, request);
  await page.getByRole('link', { name: 'Book a class' }).click();
  await expect(page.getByRole('heading', { name: 'Classes' })).toBeVisible();

  // The latest class that still has a spot: it is the furthest from its
  // cancellation cut-off, so cancelling is still open.
  // Each class is an <article> named by its title (class-card.tsx).
  const cards = page
    .getByRole('article')
    .filter({ has: page.getByRole('button', { name: 'Book', exact: true }) })
    .filter({ hasNot: page.getByText('This class is full.') });
  await expect(cards.first()).toBeVisible();
  const id = await cards.last().getAttribute('aria-labelledby');
  const card = page.locator(`[aria-labelledby="${id}"]`);
  const before = await spotsLeft(card);

  await card.getByRole('button', { name: 'Book', exact: true }).click();

  await expect(card.getByText("You're booked.")).toBeVisible();
  await expect(
    card.getByText(spots(before - 1), { exact: true }),
  ).toBeVisible();

  // The account page lists the booking under the upcoming classes (D-022).
  const className = await page.locator(`[id="${id}"]`).textContent();
  await page.goto('/account');
  const upcoming = page.getByRole('region', { name: 'Upcoming classes' });
  await expect(upcoming.getByRole('listitem')).toHaveCount(1);
  await expect(upcoming.getByRole('listitem')).toContainText(className ?? '');
  await upcoming.getByRole('link', { name: 'Book a class' }).click();
  await expect(page.getByRole('heading', { name: 'Classes' })).toBeVisible();

  await card.getByRole('button', { name: 'Cancel booking' }).click();

  await expect(
    card.getByRole('button', { name: 'Book', exact: true }),
  ).toBeVisible();
  await expect(card.getByText(spots(before), { exact: true })).toBeVisible();
});
