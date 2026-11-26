import { expect, type Page, test } from '@playwright/test';

// A first visit with nothing chosen: the app is in Danish (D-065). The other specs
// start with English chosen (e2e/english.ts).
test.use({ storageState: { cookies: [], origins: [] } });

const terms = {
  membershipName: 'General membership',
  monthlyPriceOre: 29_900,
  termsVersion: 'terms-v1',
  termsText: 'Synthetic terms text.',
  earlyStartVersion: 'early-v1',
  earlyStartText: 'Synthetic early-start text.',
};

async function mockApi(page: Page) {
  await page.route('**/api/members/signup/terms', (route) =>
    route.fulfill({ json: terms }),
  );
  await page.route('**/api/booking/schedule', (route) =>
    route.fulfill({
      json: [
        {
          classId: '0199a3c0-0000-7000-8000-000000000c01',
          className: 'Spinning',
          instructorName: 'Test Instructor',
          startsAt: '2026-11-24T17:00:00Z',
          endsAt: '2026-11-24T18:00:00Z',
          capacity: 12,
          spotsLeft: 5,
          bookedByMe: false,
          cancelUntil: '2026-11-24T05:00:00Z',
        },
      ],
    }),
  );
}

test('the landing page is in Danish with Danish formats', async ({ page }) => {
  await mockApi(page);

  await page.goto('/');

  await expect(page.locator('html')).toHaveAttribute('lang', 'da');
  await expect(
    page.getByRole('heading', { name: 'Velkommen til FitnessCenter' }),
  ).toBeVisible();
  const upcoming = page.getByRole('region', { name: 'Næste hold' });
  await expect(
    upcoming.getByText('tirsdag 24. november kl. 18:00'),
  ).toBeVisible();
  await expect(page.getByText('299,00 kr. om måneden').first()).toBeVisible();
});

test('the header switch changes to English and remembers it', async ({
  page,
}) => {
  await mockApi(page);
  await page.goto('/');

  await page
    .getByRole('radiogroup', { name: 'Sprog' })
    .getByRole('radio', { name: 'English' })
    .click();

  await expect(
    page.getByRole('heading', { name: 'Welcome to FitnessCenter' }),
  ).toBeVisible();
  await expect(page.locator('html')).toHaveAttribute('lang', 'en');

  await page.reload();
  await expect(
    page.getByRole('heading', { name: 'Welcome to FitnessCenter' }),
  ).toBeVisible();
});

test('the phone menu changes the language', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await mockApi(page);
  await page.goto('/');

  await page.getByRole('button', { name: 'Menu' }).click();
  await page.getByRole('menuitemradio', { name: /English/ }).click();

  await expect(
    page.getByRole('heading', { name: 'Welcome to FitnessCenter' }),
  ).toBeVisible();
});
