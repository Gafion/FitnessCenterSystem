import { expect, test } from '@playwright/test';

test('landing page loads', async ({ page }) => {
  await page.goto('/');

  await expect(
    page.getByRole('heading', { name: 'Welcome to FitnessCenter' }),
  ).toBeVisible();
});

// The sections below against the built frontend with the API mocked at the
// network level, like signup.spec.ts.

// A device outside Denmark: class times must still be Copenhagen time (D-039).
test.use({ timezoneId: 'America/New_York' });

const terms = {
  membershipName: 'General membership',
  monthlyPriceOre: 29_900,
  termsVersion: 'terms-v1',
  termsText: 'Synthetic terms text.',
  earlyStartVersion: 'early-v1',
  earlyStartText: 'Synthetic early-start text.',
};

function classEntry(n: number, className: string, startsAt: string) {
  return {
    classId: `0199a3c0-0000-7000-8000-000000000c0${n}`,
    className,
    instructorName: 'Test Instructor',
    startsAt,
    endsAt: startsAt,
    capacity: 12,
    spotsLeft: 5,
    bookedByMe: false,
    cancelUntil: startsAt,
  };
}

test('shows the next three classes, the membership and placeholders', async ({
  page,
}) => {
  await page.route('**/api/members/signup/terms', (route) =>
    route.fulfill({ json: terms }),
  );
  await page.route('**/api/booking/schedule', (route) =>
    route.fulfill({
      json: [
        classEntry(1, 'Spinning', '2026-11-24T17:00:00Z'),
        classEntry(2, 'Yoga', '2026-11-25T06:00:00Z'),
        classEntry(3, 'Pilates', '2026-11-26T16:00:00Z'),
        classEntry(4, 'Boxing', '2026-11-27T16:00:00Z'),
      ],
    }),
  );

  await page.goto('/');

  const upcoming = page.getByRole('region', { name: 'Next classes' });
  await expect(upcoming.getByRole('listitem')).toHaveCount(3);
  await expect(upcoming.getByText('Spinning')).toBeVisible();
  await expect(upcoming.getByText('Tuesday 24 November, 18:00')).toBeVisible();
  await expect(upcoming.getByText('Boxing')).toHaveCount(0);

  const membership = page.getByRole('region', { name: 'General membership' });
  await expect(membership.getByText('299,00 kr. per month')).toBeVisible();

  const visit = page.getByRole('region', { name: 'Visit us' });
  await expect(visit.getByText('PLACEHOLDER: opening hours')).toBeVisible();
  await expect(visit.getByText('PLACEHOLDER: address')).toBeVisible();
  await expect(visit.getByText('PLACEHOLDER: phone and email')).toBeVisible();

  await upcoming.getByRole('link', { name: 'See all classes' }).click();
  await expect(page.getByRole('heading', { name: 'Classes' })).toBeVisible();
});

test('still loads when the API fails', async ({ page }) => {
  await page.route('**/api/**', (route) => route.fulfill({ status: 500 }));

  await page.goto('/');

  await expect(
    page.getByRole('heading', { name: 'Welcome to FitnessCenter' }),
  ).toBeVisible();
  await expect(
    page.getByText('No classes are scheduled right now.'),
  ).toBeVisible();
  await expect(page.getByText(/per month/)).toHaveCount(0);
});
