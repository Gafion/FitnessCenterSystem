import { fireEvent, render, screen, within } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import '@/i18n';
import { Schedule } from './schedule';
import { beforeCutOff, classEntry } from './test-entries';

const noop = () => Promise.resolve(undefined);

test('groups classes by their day in Copenhagen', () => {
  const entries = [
    classEntry(),
    // 23:30 UTC on the 24th is already 00:30 on the 25th in Copenhagen.
    classEntry({
      classId: '0199a3c0-0000-7000-8000-000000000c02',
      className: 'Yoga',
      startsAt: '2026-11-24T23:30:00Z',
      endsAt: '2026-11-25T00:30:00Z',
      cancelUntil: '2026-11-24T11:30:00Z',
    }),
  ];
  render(
    <Schedule
      entries={entries}
      now={beforeCutOff}
      onBook={noop}
      onCancel={noop}
    />,
  );

  const tuesday = screen.getByRole('region', { name: 'Tuesday 24 November' });
  const wednesday = screen.getByRole('region', {
    name: 'Wednesday 25 November',
  });
  expect(within(tuesday).getByText('Spinning')).toBeInTheDocument();
  expect(within(wednesday).getByText('Yoga')).toBeInTheDocument();
  expect(within(wednesday).getByText('00:30–01:30')).toBeInTheDocument();
});

test('books the class whose button was pressed', () => {
  const onBook = vi.fn().mockResolvedValue(undefined);
  render(
    <Schedule
      entries={[classEntry()]}
      now={beforeCutOff}
      onBook={onBook}
      onCancel={noop}
    />,
  );

  fireEvent.click(screen.getByRole('button', { name: 'Book' }));

  expect(onBook).toHaveBeenCalledWith('0199a3c0-0000-7000-8000-000000000c01');
});

test('an empty schedule says so', () => {
  render(
    <Schedule entries={[]} now={beforeCutOff} onBook={noop} onCancel={noop} />,
  );

  expect(
    screen.getByText('There are no classes in the next 14 days.'),
  ).toBeInTheDocument();
});
