import { render, screen, within } from '@testing-library/react';
import { expect, test } from 'vitest';
import '@/i18n';
import type { Schemas } from '@/lib/api';
import { MyBookings } from './my-bookings';

// Synthetic bookings. 17:00Z on 24 Nov is 18:00 in Copenhagen (winter time, UTC+1);
// 16:00Z on 1 Jul is also 18:00 there (summer time, UTC+2).
const bookings: Schemas['MyBooking'][] = [
  {
    classId: '0199a3c0-0000-7000-8000-0000000000c1',
    className: 'Spinning',
    instructorName: 'Test Instructor',
    startsAt: '2026-11-24T17:00:00Z',
    endsAt: '2026-11-24T17:45:00Z',
    cancelUntil: '2026-11-24T05:00:00Z',
  },
  {
    classId: '0199a3c0-0000-7000-8000-0000000000c2',
    className: 'Yoga',
    instructorName: 'Other Instructor',
    startsAt: '2027-07-01T16:00:00Z',
    endsAt: '2027-07-01T17:00:00Z',
    cancelUntil: '2027-07-01T04:00:00Z',
  },
];

test('lists each booked class with its Copenhagen time, in the order given', () => {
  render(<MyBookings bookings={bookings} />);

  const items = screen.getAllByRole('listitem');
  expect(items).toHaveLength(2);
  expect(items[0]).toHaveTextContent('Spinning');
  expect(items[0]).toHaveTextContent('Tue 24 Nov, 18:00–18:45, Danish time');
  expect(
    within(items[0]).getByText('with Test Instructor'),
  ).toBeInTheDocument();
  expect(items[1]).toHaveTextContent('Yoga');
  expect(items[1]).toHaveTextContent('Thu 1 Jul, 18:00–19:00, Danish time');
});

test('says when nothing is booked', () => {
  render(<MyBookings bookings={[]} />);

  expect(screen.getByText('You have no upcoming classes.')).toBeInTheDocument();
  expect(screen.queryByRole('list')).not.toBeInTheDocument();
});
