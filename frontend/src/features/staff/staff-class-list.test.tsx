import {
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import type { Schemas } from '@/lib/api';
import { StaffClassList } from './staff-class-list';

// Synthetic classes. 17:00Z on 1 Dec is 18:00 in Copenhagen (winter time, UTC+1).
const now = new Date('2026-11-30T12:00:00Z');
const classTypes: Schemas['StaffClassType'][] = [
  { id: '0199a3c0-0000-7000-8000-0000000000a1', name: 'Spinning' },
];
const instructors: Schemas['StaffMember'][] = [
  { id: '0199a3c0-0000-7000-8000-0000000000b1', displayName: 'Ida I.' },
];

function staffClass(
  overrides: Partial<Schemas['StaffClass']>,
): Schemas['StaffClass'] {
  return {
    classId: '0199a3c0-0000-7000-8000-0000000000c1',
    classTypeId: classTypes[0].id,
    className: 'Spinning',
    instructorAccountId: instructors[0].id,
    instructorName: 'Ida I.',
    startsAt: '2026-12-01T17:00:00Z',
    endsAt: '2026-12-01T17:45:00Z',
    lengthMinutes: 45,
    capacity: 12,
    bookedCount: 3,
    waitlistCount: 0,
    status: 'Scheduled',
    cancelledAt: null,
    ...overrides,
  };
}

function renderList(
  classes: Schemas['StaffClass'][],
  onCancel = vi.fn().mockResolvedValue('ok'),
) {
  render(
    <StaffClassList
      classes={classes}
      classTypes={classTypes}
      instructors={instructors}
      now={now}
      onChange={vi.fn().mockResolvedValue('saved')}
      onCancel={onCancel}
    />,
  );
  return onCancel;
}

test('lists the classes by day with instructor, bookings and waitlist', () => {
  renderList([
    staffClass({ waitlistCount: 2, bookedCount: 12 }),
    staffClass({
      classId: '0199a3c0-0000-7000-8000-0000000000c2',
      className: 'Yoga',
      status: 'Cancelled',
      cancelledAt: '2026-11-29T10:00:00Z',
      startsAt: '2026-12-02T07:00:00Z',
      endsAt: '2026-12-02T08:00:00Z',
    }),
  ]);

  expect(
    screen.getAllByRole('heading', { level: 3 }).map((h) => h.textContent),
  ).toEqual(['Tuesday 1 December', 'Wednesday 2 December']);
  const spinning = screen.getByRole('listitem', { name: 'Spinning' });
  expect(spinning).toHaveTextContent('18:00–18:45');
  expect(spinning).toHaveTextContent('Ida I.');
  expect(spinning).toHaveTextContent('12 of 12 spots booked');
  expect(spinning).toHaveTextContent('2 on the waitlist');

  const yoga = screen.getByRole('listitem', { name: 'Yoga' });
  expect(yoga).toHaveTextContent('Cancelled');
  expect(within(yoga).queryByRole('button')).not.toBeInTheDocument();
});

test('cancelling asks first and says how many booked members get an email', async () => {
  const onCancel = renderList([staffClass({ waitlistCount: 1 })]);
  const item = screen.getByRole('listitem', { name: 'Spinning' });

  fireEvent.click(within(item).getByRole('button', { name: 'Cancel class' }));

  expect(item).toHaveTextContent('Cancel Spinning, Tue 1 Dec, 18:00?');
  expect(item).toHaveTextContent(
    'The 3 booked members get an email that the class is cancelled, and their bookings are cancelled. The waitlist is cleared.',
  );
  expect(onCancel).not.toHaveBeenCalled();

  fireEvent.click(
    within(item).getByRole('button', { name: 'Yes, cancel the class' }),
  );
  await waitFor(() =>
    expect(onCancel).toHaveBeenCalledWith(
      '0199a3c0-0000-7000-8000-0000000000c1',
    ),
  );
});

test('keeping the class closes the question without cancelling', () => {
  const onCancel = renderList([staffClass({ bookedCount: 0 })]);
  const item = screen.getByRole('listitem', { name: 'Spinning' });

  fireEvent.click(within(item).getByRole('button', { name: 'Cancel class' }));
  expect(item).toHaveTextContent('No one has booked, so no one gets an email.');
  fireEvent.click(within(item).getByRole('button', { name: 'Keep the class' }));

  expect(
    within(item).getByRole('button', { name: 'Cancel class' }),
  ).toBeInTheDocument();
  expect(onCancel).not.toHaveBeenCalled();
});

test('a refused cancellation says why', async () => {
  renderList(
    [staffClass({ bookedCount: 1 })],
    vi.fn().mockResolvedValue('class-started'),
  );
  const item = screen.getByRole('listitem', { name: 'Spinning' });

  fireEvent.click(within(item).getByRole('button', { name: 'Cancel class' }));
  expect(item).toHaveTextContent(
    'The 1 booked member gets an email that the class is cancelled, and the booking is cancelled.',
  );
  fireEvent.click(
    within(item).getByRole('button', { name: 'Yes, cancel the class' }),
  );

  expect(await within(item).findByRole('alert')).toHaveTextContent(
    "The class has started, so it can't be changed.",
  );
});

test('a class that has started can be neither edited nor cancelled', () => {
  renderList([
    staffClass({
      startsAt: '2026-11-30T11:30:00Z',
      endsAt: '2026-11-30T12:30:00Z',
    }),
  ]);
  const item = screen.getByRole('listitem', { name: 'Spinning' });

  expect(within(item).queryByRole('button')).not.toBeInTheDocument();
  expect(item).toHaveTextContent(
    "This class has started and can't be changed.",
  );
});

test('edit opens the form for that class', () => {
  renderList([staffClass({})]);
  const item = screen.getByRole('listitem', { name: 'Spinning' });

  fireEvent.click(within(item).getByRole('button', { name: 'Edit' }));

  expect(
    within(item).getByRole('form', { name: 'Edit Spinning' }),
  ).toBeInTheDocument();
  expect(within(item).getByLabelText('Spots')).toHaveValue(12);
});

test('says when no classes are scheduled', () => {
  renderList([]);
  expect(
    screen.getByText('No classes are scheduled in this period.'),
  ).toBeInTheDocument();
});
