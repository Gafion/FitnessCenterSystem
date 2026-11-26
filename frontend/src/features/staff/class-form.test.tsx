import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import type { Schemas } from '@/lib/api';
import { ClassForm } from './class-form';

// Synthetic class types, instructors and class.
const classTypes: Schemas['StaffClassType'][] = [
  { id: '0199a3c0-0000-7000-8000-0000000000a1', name: 'Spinning' },
  { id: '0199a3c0-0000-7000-8000-0000000000a2', name: 'Yoga' },
];
const instructors: Schemas['StaffMember'][] = [
  { id: '0199a3c0-0000-7000-8000-0000000000b1', displayName: 'Åse Å.' },
  { id: '0199a3c0-0000-7000-8000-0000000000b2', displayName: 'Ida I.' },
  { id: '0199a3c0-0000-7000-8000-0000000000b3', displayName: 'Øjvind Ø.' },
  { id: '0199a3c0-0000-7000-8000-0000000000b4', displayName: 'Ærle Æ.' },
  { id: '0199a3c0-0000-7000-8000-0000000000b5', displayName: 'Bo B.' },
];
const booked: Schemas['StaffClass'] = {
  classId: '0199a3c0-0000-7000-8000-0000000000c1',
  classTypeId: classTypes[0].id,
  className: 'Spinning',
  instructorAccountId: instructors[1].id,
  instructorName: 'Ida I.',
  // 18:00 in Copenhagen (winter time, UTC+1).
  startsAt: '2026-12-01T17:00:00Z',
  endsAt: '2026-12-01T17:45:00Z',
  lengthMinutes: 45,
  capacity: 12,
  bookedCount: 3,
  waitlistCount: 0,
  status: 'Scheduled',
  cancelledAt: null,
};

function renderForm(props: Partial<Parameters<typeof ClassForm>[0]> = {}) {
  const onSubmit = props.onSubmit ?? vi.fn().mockResolvedValue('saved');
  const onClose = props.onClose ?? vi.fn();
  render(
    <ClassForm
      classTypes={classTypes}
      instructors={instructors}
      {...props}
      onSubmit={onSubmit}
      onClose={onClose}
    />,
  );
  return { onSubmit, onClose };
}

/** Opens a picker from the keyboard, as jsdom has no pointer layout. */
function openPicker(label: string) {
  fireEvent.keyDown(screen.getByRole('combobox', { name: label }), {
    key: 'ArrowDown',
  });
}

async function choose(label: string, option: string) {
  openPicker(label);
  fireEvent.click(await screen.findByRole('option', { name: option }));
}

function fill(label: string, value: string) {
  fireEvent.change(screen.getByLabelText(label), { target: { value } });
}

test('sorts the instructors the Danish way: Æ, Ø and Å last', async () => {
  renderForm();

  openPicker('Instructor');
  const options = await screen.findAllByRole('option');
  expect(options.map((option) => option.textContent)).toEqual([
    'Bo B.',
    'Ida I.',
    'Ærle Æ.',
    'Øjvind Ø.',
    'Åse Å.',
  ]);
});

test('schedules a class from Copenhagen time', async () => {
  const { onSubmit, onClose } = renderForm();

  await choose('Class type', 'Yoga');
  await choose('Instructor', 'Bo B.');
  fill('Date', '2027-07-01');
  fill('Time', '18:00');
  fill('Length in minutes', '60');
  fill('Spots', '10');
  fireEvent.click(screen.getByRole('button', { name: 'Schedule class' }));

  await waitFor(() => expect(onClose).toHaveBeenCalled());
  expect(onSubmit).toHaveBeenCalledWith({
    classTypeId: classTypes[1].id,
    instructorAccountId: instructors[4].id,
    // 18:00 in Copenhagen is 16:00 UTC in summer.
    startsAt: '2027-07-01T16:00:00.000Z',
    lengthMinutes: 60,
    capacity: 10,
  });
});

test('says what is missing before it calls the API', async () => {
  const { onSubmit } = renderForm();

  fill('Length in minutes', '0');
  fill('Spots', '1001');
  fireEvent.click(screen.getByRole('button', { name: 'Schedule class' }));

  expect(await screen.findByText('Choose a class type.')).toBeInTheDocument();
  expect(screen.getByText('Choose an instructor.')).toBeInTheDocument();
  expect(screen.getByText('Enter a date and a time.')).toBeInTheDocument();
  expect(
    screen.getByText('Enter a whole number of minutes from 1 to 1440.'),
  ).toBeInTheDocument();
  expect(
    screen.getByText('Enter a whole number of spots from 1 to 1000.'),
  ).toBeInTheDocument();
  expect(onSubmit).not.toHaveBeenCalled();
});

test('editing starts from the class and keeps the capacity at or above the bookings', async () => {
  const { onSubmit } = renderForm({ editing: booked });

  expect(
    screen.getByRole('combobox', { name: 'Class type' }),
  ).toHaveTextContent('Spinning');
  expect(
    screen.getByRole('combobox', { name: 'Instructor' }),
  ).toHaveTextContent('Ida I.');
  expect(screen.getByLabelText('Date')).toHaveValue('2026-12-01');
  expect(screen.getByLabelText('Time')).toHaveValue('18:00');
  expect(screen.getByLabelText('Length in minutes')).toHaveValue(45);
  const spots = screen.getByLabelText('Spots');
  expect(spots).toHaveValue(12);
  expect(spots).toHaveAttribute('min', '3');
  expect(spots).toHaveAccessibleDescription(
    'At least 3: 3 members have booked.',
  );

  fill('Spots', '2');
  fireEvent.click(screen.getByRole('button', { name: 'Save changes' }));

  expect(
    await screen.findByText(
      'The class has 3 bookings, so it needs at least 3 spots.',
    ),
  ).toBeInTheDocument();
  expect(onSubmit).not.toHaveBeenCalled();
});

test('moving a booked class says the members will be emailed the new time', () => {
  renderForm({ editing: booked });
  expect(screen.queryByRole('status')).not.toBeInTheDocument();

  fill('Time', '19:00');

  expect(screen.getByRole('status')).toHaveTextContent(
    'The 3 booked members will be emailed the new time and may then cancel until the class starts.',
  );

  fill('Time', '18:00');
  expect(screen.queryByRole('status')).not.toBeInTheDocument();
});

test('moving a class no one booked says nothing about emails', () => {
  renderForm({ editing: { ...booked, bookedCount: 0 } });

  fill('Date', '2026-12-02');

  expect(screen.queryByRole('status')).not.toBeInTheDocument();
  expect(screen.getByLabelText('Spots')).toHaveAttribute('min', '1');
});

test('shows the API errors on their fields and keeps the form open', async () => {
  const onSubmit = vi.fn().mockResolvedValue({
    errors: {
      startsAt: ['The class must start in the future.'],
      instructorAccountId: ['That account is not an instructor.'],
      '': ['More members have booked than that.'],
    },
  });
  const { onClose } = renderForm({ editing: booked, onSubmit });

  fireEvent.click(screen.getByRole('button', { name: 'Save changes' }));

  expect(
    await screen.findByText('The class must start in the future.'),
  ).toBeInTheDocument();
  expect(screen.getByLabelText('Date')).toHaveAttribute('aria-invalid', 'true');
  expect(
    screen.getByText('That account is not an instructor.'),
  ).toBeInTheDocument();
  expect(
    screen.getByText('More members have booked than that.'),
  ).toBeInTheDocument();
  expect(onClose).not.toHaveBeenCalled();
});
