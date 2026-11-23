import { expect, type Page, test } from '@playwright/test';

// The payment simulator and the account page's payment history against the built
// frontend, with the API mocked at the network level like classes.spec.ts. The payment
// rules themselves are covered by the backend's tests (PaymentTests, MemberPaymentTests).

const account = {
  accountId: '0199a3c0-0000-7000-8000-0000000000aa',
  email: 'test.person@example.com',
  firstName: 'Test',
  lastName: 'Person',
  hasGoogleLogin: false,
};

type Payment = {
  amountOre: number;
  paidAt: string;
  cardLast4: string;
  periodFrom: string;
  periodTo: string;
};

/** A fake API holding one member's payments; the simulator appends to them. */
async function mockApi(page: Page, options: { simulation: boolean }) {
  const payments: Payment[] = [];
  const sent: unknown[] = [];
  const myPayments = () => ({
    status: payments.length ? 'Active' : 'AwaitingFirstPayment',
    paidUntil: payments[0]?.periodTo ?? null,
    monthlyPriceOre: 29_900,
    payments,
  });
  await page.route('**/api/identity/me', (route) =>
    route.fulfill({ json: account }),
  );
  await page.route('**/api/identity/login-options', (route) =>
    route.fulfill({ json: { google: false } }),
  );
  await page.route('**/api/simulation', (route) =>
    route.fulfill({ json: { enabled: options.simulation } }),
  );
  await page.route('**/api/members/me/payments', (route) =>
    route.fulfill({ json: myPayments() }),
  );
  await page.route('**/api/simulation/payments', async (route) => {
    const body = route.request().postDataJSON() as {
      periodFrom: string | null;
    };
    sent.push(body);
    if (payments.length && body.periodFrom === '2026-01-01') {
      return route.fulfill({
        status: 409,
        json: { type: 'period-not-contiguous' },
      });
    }
    const payment = body.periodFrom
      ? {
          periodFrom: body.periodFrom,
          periodTo: '2026-09-30',
        }
      : { periodFrom: '2026-10-01', periodTo: '2026-10-31' };
    payments.unshift({
      amountOre: 29_900,
      paidAt: '2026-11-23T12:00:00Z',
      cardLast4: '4242',
      ...payment,
    });
    return route.fulfill({ status: 201, json: payments[0] });
  });
  return { sent };
}

test('pays a past month, then the next, and the account page lists both', async ({
  page,
}) => {
  const { sent } = await mockApi(page, { simulation: true });

  await page.goto('/account');
  await expect(page.getByText('No payments yet.')).toBeVisible();
  await page.getByRole('link', { name: 'Simulated payments' }).click();

  await expect(page.getByRole('complementary', { name: 'Simulation' })).toBeVisible();
  await page.getByLabel('Or pay one month starting on').fill('2026-09-01');
  await page.getByRole('button', { name: 'Pay for this month' }).click();
  await expect(
    page.getByText('Paid until 30 Sept 2026.').first(),
  ).toBeVisible();

  await page.getByRole('button', { name: 'Pay next month' }).click();
  await expect(page.getByText('Paid until 31 Oct 2026.').first()).toBeVisible();
  expect(sent).toEqual([{ periodFrom: '2026-09-01' }, { periodFrom: null }]);

  await page.getByRole('link', { name: 'Back to your account' }).click();
  const history = page.getByRole('table');
  await expect(history.getByRole('row')).toHaveCount(3);
  await expect(history.getByRole('row').nth(1)).toContainText(
    '1 Oct 2026 – 31 Oct 2026',
  );
  await expect(history.getByRole('row').nth(2)).toContainText('ending 4242');
});

test('a refused period is explained', async ({ page }) => {
  await mockApi(page, { simulation: true });
  await page.goto('/simulation/payments');
  await page.getByRole('button', { name: 'Pay next month' }).click();
  await expect(page.getByText('Paid until 31 Oct 2026.').first()).toBeVisible();

  await page.getByLabel('Or pay one month starting on').fill('2026-01-01');
  await page.getByRole('button', { name: 'Pay for this month' }).click();

  await expect(page.getByRole('alert')).toHaveText(
    'A payment must start the day after the paid-until date.',
  );
});

test('the simulator is hidden when the simulation is off', async ({ page }) => {
  await mockApi(page, { simulation: false });

  await page.goto('/account');
  await expect(page.getByRole('heading', { name: 'Payments' })).toBeVisible();
  await expect(
    page.getByRole('link', { name: 'Simulated payments' }),
  ).toHaveCount(0);

  await page.goto('/simulation/payments');
  await expect(page.getByRole('complementary', { name: 'Simulation' })).toHaveCount(0);
  await expect(
    page.getByRole('button', { name: 'Pay next month' }),
  ).toHaveCount(0);
});
