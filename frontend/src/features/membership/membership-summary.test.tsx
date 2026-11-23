import { render, screen } from '@testing-library/react';
import { expect, test } from 'vitest';
import '@/i18n';
import type { Schemas } from '@/lib/api';
import { MembershipSummary } from './membership-summary';

const newMembership: Schemas['MyMembership'] = {
  membershipType: 'General membership',
  monthlyPriceOre: 29_900,
  status: 'AwaitingFirstPayment',
  memberSince: '2026-11-23',
  paidUntil: null,
};

/** The value next to a term in the summary's description list. */
function valueNextTo(term: string): string | null {
  return screen.getByText(term).nextElementSibling?.textContent ?? null;
}

test('a new member sees the type, price, status and signup date', () => {
  render(<MembershipSummary membership={newMembership} />);

  expect(valueNextTo('Type')).toBe('General membership');
  expect(valueNextTo('Price')).toMatch(/^299,00\s*kr\. per month$/);
  expect(valueNextTo('Status')).toBe('Awaiting first payment');
  expect(valueNextTo('Member since')).toBe('23 Nov 2026');
  expect(screen.queryByText('Paid until')).not.toBeInTheDocument();
});

test('a paying member also sees the paid-until date', () => {
  render(
    <MembershipSummary
      membership={{
        ...newMembership,
        status: 'Active',
        paidUntil: '2026-12-22',
      }}
    />,
  );

  expect(valueNextTo('Status')).toBe('Active');
  expect(valueNextTo('Paid until')).toBe('22 Dec 2026');
});
