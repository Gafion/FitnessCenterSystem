import type { Schemas } from '@/lib/api';

/** A paid member with two payments, newest first, as the API sends them. Synthetic data. */
export function paidMember(
  overrides: Partial<Schemas['MyPayments']> = {},
): Schemas['MyPayments'] {
  return {
    status: 'Active',
    paidUntil: '2027-01-22',
    monthlyPriceOre: 29_900,
    payments: [
      {
        amountOre: 29_900,
        paidAt: '2026-11-23T12:05:00Z',
        cardLast4: '4242',
        periodFrom: '2026-12-23',
        periodTo: '2027-01-22',
      },
      {
        amountOre: 29_900,
        paidAt: '2026-11-23T12:00:00Z',
        cardLast4: '4242',
        periodFrom: '2026-11-23',
        periodTo: '2026-12-22',
      },
    ],
    ...overrides,
  };
}

export const newMember: Schemas['MyPayments'] = {
  status: 'AwaitingFirstPayment',
  paidUntil: null,
  monthlyPriceOre: 29_900,
  payments: [],
};
