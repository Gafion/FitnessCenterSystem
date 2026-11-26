import { render, screen, within } from '@testing-library/react';
import { expect, test } from 'vitest';
import '@/i18n';
import type { Schemas } from '@/lib/api';
import { MyClasses } from './my-classes';

// Synthetic classes and names. 17:00Z on 24 Nov is 18:00 in Copenhagen (winter time,
// UTC+1); 16:00Z on 1 Jul is also 18:00 there (summer time, UTC+2).
const classes: Schemas['MyClass'][] = [
  {
    classId: '0199a3c0-0000-7000-8000-0000000000c1',
    className: 'Spinning',
    startsAt: '2026-11-24T17:00:00Z',
    endsAt: '2026-11-24T17:45:00Z',
    capacity: 12,
    bookedNames: ['Alex E.', 'Sam S.', 'Alex E.'],
  },
  {
    classId: '0199a3c0-0000-7000-8000-0000000000c2',
    className: 'Yoga',
    startsAt: '2026-11-24T19:00:00Z',
    endsAt: '2026-11-24T20:00:00Z',
    capacity: 1,
    bookedNames: ['Kim P.'],
  },
  {
    classId: '0199a3c0-0000-7000-8000-0000000000c3',
    className: 'Pilates',
    startsAt: '2027-07-01T16:00:00Z',
    endsAt: '2027-07-01T17:00:00Z',
    capacity: 8,
    bookedNames: [],
  },
];

test('groups the classes by Copenhagen day, in the order given', () => {
  render(<MyClasses classes={classes} />);

  const days = screen.getAllByRole('heading', { level: 2 });
  expect(days.map((day) => day.textContent)).toEqual([
    'Tuesday 24 November',
    'Thursday 1 July',
  ]);
  expect(
    screen.getAllByRole('heading', { level: 3 }).map((h) => h.textContent),
  ).toEqual(['Spinning', 'Yoga', 'Pilates']);
});

test('shows each class with its time, capacity and booked names', () => {
  render(<MyClasses classes={classes} />);

  const spinning = screen.getByRole('listitem', { name: 'Spinning' });
  expect(spinning).toHaveTextContent('18:00–18:45');
  expect(spinning).toHaveTextContent('3 of 12 spots booked');
  const names = within(spinning).getByRole('list', {
    name: 'Booked for Spinning',
  });
  // Two members can share a display name; both are listed.
  expect(
    within(names)
      .getAllByRole('listitem')
      .map((item) => item.textContent),
  ).toEqual(['Alex E.', 'Sam S.', 'Alex E.']);

  const yoga = screen.getByRole('listitem', { name: 'Yoga' });
  expect(yoga).toHaveTextContent('20:00–21:00');
  expect(yoga).toHaveTextContent('1 of 1 spot booked');
});

test('says when no one has booked a class yet', () => {
  render(<MyClasses classes={classes} />);

  const pilates = screen.getByRole('listitem', { name: 'Pilates' });
  expect(pilates).toHaveTextContent('18:00–19:00');
  expect(pilates).toHaveTextContent('0 of 8 spots booked');
  expect(pilates).toHaveTextContent('No one has booked yet.');
  expect(within(pilates).queryByRole('list')).not.toBeInTheDocument();
});

test('says when there are no upcoming classes', () => {
  render(<MyClasses classes={[]} />);

  expect(screen.getByText('You have no upcoming classes.')).toBeInTheDocument();
  expect(screen.queryByRole('list')).not.toBeInTheDocument();
});

test('the plural follows the capacity, not the number booked', () => {
  render(
    <MyClasses
      classes={[
        {
          classId: '0199a3c0-0000-7000-8000-0000000000c4',
          className: 'Boxing',
          startsAt: '2026-11-24T17:00:00Z',
          endsAt: '2026-11-24T17:45:00Z',
          capacity: 5,
          bookedNames: ['Kim P.'],
        },
      ]}
    />,
  );

  expect(screen.getByRole('listitem', { name: 'Boxing' })).toHaveTextContent(
    '1 of 5 spots booked',
  );
});
