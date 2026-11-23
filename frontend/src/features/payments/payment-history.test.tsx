import { render, screen, within } from '@testing-library/react';
import { expect, test } from 'vitest';
import '@/i18n';
import { PaymentHistory } from './payment-history';
import { newMember, paidMember } from './test-payments';

test('lists each payment with its period, amount and last card digits', () => {
  render(<PaymentHistory payments={paidMember()} />);

  expect(screen.getByText('Paid until 22 Jan 2027.')).toBeInTheDocument();
  const rows = within(screen.getByRole('table')).getAllByRole('row');
  expect(rows).toHaveLength(3);
  expect(rows[1]).toHaveTextContent('23 Dec 2026 – 22 Jan 2027');
  expect(rows[1]).toHaveTextContent(/299,00\s*kr\./);
  expect(rows[1]).toHaveTextContent('ending 4242');
  expect(rows[2]).toHaveTextContent('23 Nov 2026 – 22 Dec 2026');
});

test('a new member sees that no payment has arrived yet', () => {
  render(<PaymentHistory payments={newMember} />);

  expect(
    screen.getByText(
      'No payment yet. Your membership starts with the first payment.',
    ),
  ).toBeInTheDocument();
  expect(screen.getByText('No payments yet.')).toBeInTheDocument();
  expect(screen.queryByRole('table')).not.toBeInTheDocument();
});
