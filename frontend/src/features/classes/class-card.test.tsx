import { fireEvent, render, screen } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import '@/i18n';
import { ClassCard } from './class-card';
import { afterCutOff, beforeCutOff, classEntry } from './test-entries';

const noop = () => Promise.resolve(undefined);

test('shows the name, instructor, Copenhagen time and spots left', () => {
  render(
    <ClassCard
      entry={classEntry()}
      now={beforeCutOff}
      onBook={noop}
      onCancel={noop}
    />,
  );

  expect(screen.getByText('Spinning')).toBeInTheDocument();
  expect(screen.getByText('with Test Instructor')).toBeInTheDocument();
  expect(screen.getByText('18:00–18:45')).toBeInTheDocument();
  expect(screen.getByText('5 spots left')).toBeInTheDocument();
});

test('says "1 spot left" for the last spot', () => {
  render(
    <ClassCard
      entry={classEntry({ spotsLeft: 1 })}
      now={beforeCutOff}
      onBook={noop}
      onCancel={noop}
    />,
  );

  expect(screen.getByText('1 spot left')).toBeInTheDocument();
});

test('the Book button books', () => {
  const onBook = vi.fn().mockResolvedValue(undefined);
  render(
    <ClassCard
      entry={classEntry()}
      now={beforeCutOff}
      onBook={onBook}
      onCancel={noop}
    />,
  );

  fireEvent.click(screen.getByRole('button', { name: 'Book' }));

  expect(onBook).toHaveBeenCalledOnce();
});

test('a full class says so and cannot be booked', () => {
  render(
    <ClassCard
      entry={classEntry({ spotsLeft: 0 })}
      now={beforeCutOff}
      onBook={noop}
      onCancel={noop}
    />,
  );

  expect(screen.getByText('This class is full.')).toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Book' })).toBeDisabled();
});

test('a class after the membership ends says why and cannot be booked', () => {
  render(
    <ClassCard
      entry={classEntry({ afterMyMembershipEnds: true })}
      now={beforeCutOff}
      onBook={noop}
      onCancel={noop}
    />,
  );

  expect(
    screen.getByText(
      "Your membership ends before this class, so you can't book it.",
    ),
  ).toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Book' })).toBeDisabled();
});

test.each([
  ['class-full', 'This class is full.'],
  ['class-started', 'This class has already started.'],
  ['booking-not-open', 'Booking opens 14 days before the class.'],
  [
    'after-membership-ends',
    "Your membership ends before this class, so you can't book it.",
  ],
  ['already-booked', 'You have already booked this class.'],
  ['something-new', 'Something went wrong. Please try again.'],
])('a refused booking (%s) shows why', async (type, message) => {
  render(
    <ClassCard
      entry={classEntry()}
      now={beforeCutOff}
      onBook={vi.fn().mockResolvedValue(type)}
      onCancel={noop}
    />,
  );

  fireEvent.click(screen.getByRole('button', { name: 'Book' }));

  expect(await screen.findByRole('alert')).toHaveTextContent(message);
});

test('an own booking shows the cancel cut-off and can be cancelled', () => {
  const onCancel = vi.fn().mockResolvedValue(undefined);
  render(
    <ClassCard
      entry={classEntry({ bookedByMe: true, spotsLeft: 0 })}
      now={beforeCutOff}
      onBook={noop}
      onCancel={onCancel}
    />,
  );

  expect(screen.getByText("You're booked.")).toBeInTheDocument();
  expect(
    screen.getByText('You can cancel until Tue 24 Nov, 06:00.'),
  ).toBeInTheDocument();
  expect(screen.queryByText('This class is full.')).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'Cancel booking' }));

  expect(onCancel).toHaveBeenCalledOnce();
});

test('after the cut-off, cancelling shows as closed', () => {
  render(
    <ClassCard
      entry={classEntry({ bookedByMe: true })}
      now={afterCutOff}
      onBook={noop}
      onCancel={noop}
    />,
  );

  expect(
    screen.getByText('Cancelling closed at Tue 24 Nov, 06:00.'),
  ).toBeInTheDocument();
  expect(
    screen.queryByRole('button', { name: 'Cancel booking' }),
  ).not.toBeInTheDocument();
});

test('a refused cancel says cancelling has closed', async () => {
  render(
    <ClassCard
      entry={classEntry({ bookedByMe: true })}
      now={beforeCutOff}
      onBook={noop}
      onCancel={vi.fn().mockResolvedValue('cancellation-closed')}
    />,
  );

  fireEvent.click(screen.getByRole('button', { name: 'Cancel booking' }));

  expect(await screen.findByRole('alert')).toHaveTextContent(
    'Cancelling closes 12 hours before the class starts.',
  );
});
