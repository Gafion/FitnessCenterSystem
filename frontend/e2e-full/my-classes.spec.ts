import { expect, type Locator, type Page, test } from '@playwright/test';
import { signUpAndLogIn } from './member';
import { devInstructor, logInAsStaff } from './staff';

// Full stack (D-043): the seeded instructor teaches every development class (D-060)
// and sees who booked them (D-049). Other runs may have booked the same class, so the
// test reads the instructor's count first and checks that it moves by one.

async function bookedCount(item: Locator): Promise<number> {
  const text = await item.getByText(/^\d+ of \d+ spots? booked$/).textContent();
  return Number.parseInt(text ?? '', 10);
}

async function openMyClasses(page: Page) {
  await page.getByRole('link', { name: 'Your classes' }).click();
  await expect(
    page.getByRole('heading', { name: 'Your classes', level: 1 }),
  ).toBeVisible();
}

test('the instructor sees a new booking on their classes page', async ({
  page,
  browser,
  request,
  baseURL,
}) => {
  // The member picks the latest class with a free spot, as in booking.spec.ts.
  await signUpAndLogIn(page, request);
  await page.goto('/classes');
  const cards = page
    .getByRole('article')
    .filter({ has: page.getByRole('button', { name: 'Book', exact: true }) })
    .filter({ hasNot: page.getByText('This class is full.') });
  await expect(cards.first()).toBeVisible();
  const id = await cards.last().getAttribute('aria-labelledby');
  const card = page.locator(`[aria-labelledby="${id}"]`);

  // The instructor logs in in a separate browser, with its own cookies.
  // A new context doesn't take the config's options, so it gets the base URL here.
  const staffContext = await browser.newContext({ baseURL });
  const staff = await staffContext.newPage();
  let booked = false;
  try {
    await logInAsStaff(staff, devInstructor);
    await openMyClasses(staff);
    // Both pages name a class by "class-<id>" (class-card.tsx, my-classes.tsx).
    const item = staff.locator(`li[aria-labelledby="${id}"]`);
    await expect(item).toBeVisible();
    const before = await bookedCount(item);

    await card.getByRole('button', { name: 'Book', exact: true }).click();
    await expect(card.getByText("You're booked.")).toBeVisible();
    booked = true;

    await staff.reload();
    await expect(item.getByText(/spots? booked$/)).toHaveText(
      new RegExp(`^${before + 1} of \\d+ spots? booked$`),
    );
    // Display names only: the signup helper's "Test Member" shows as "Test M.".
    await expect(item.getByRole('list').getByRole('listitem')).toContainText([
      'Test M.',
    ]);
  } finally {
    // Leave the class as it was for other runs.
    if (booked) {
      await card.getByRole('button', { name: 'Cancel booking' }).click();
      await expect(
        card.getByRole('button', { name: 'Book', exact: true }),
      ).toBeVisible();
    }
    await staffContext.close();
  }
});
