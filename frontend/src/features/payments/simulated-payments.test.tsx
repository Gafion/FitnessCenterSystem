import { fireEvent, render, screen } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import '@/i18n';
import { SimulatedPayments } from './simulated-payments';
import { paidMember } from './test-payments';

test('"Pay next month" pays without a start date', async () => {
  const onPay = vi.fn().mockResolvedValue(undefined);
  render(<SimulatedPayments payments={paidMember()} onPay={onPay} />);

  fireEvent.click(screen.getByRole('button', { name: 'Pay next month' }));

  expect(onPay).toHaveBeenCalledWith(undefined);
  expect(
    await screen.findByText('Paid until 22 Jan 2027.'),
  ).toBeInTheDocument();
});

test('pays one month from a chosen date', () => {
  const onPay = vi.fn().mockResolvedValue(undefined);
  render(<SimulatedPayments payments={paidMember()} onPay={onPay} />);

  fireEvent.change(screen.getByLabelText('Or pay one month starting on'), {
    target: { value: '2026-09-01' },
  });
  fireEvent.click(screen.getByRole('button', { name: 'Pay for this month' }));

  expect(onPay).toHaveBeenCalledWith('2026-09-01');
});

test('explains a refused period', async () => {
  const onPay = vi.fn().mockResolvedValue('period-not-contiguous');
  render(<SimulatedPayments payments={paidMember()} onPay={onPay} />);

  fireEvent.click(screen.getByRole('button', { name: 'Pay next month' }));

  expect(await screen.findByRole('alert')).toHaveTextContent(
    'A payment must start the day after the paid-until date.',
  );
});
