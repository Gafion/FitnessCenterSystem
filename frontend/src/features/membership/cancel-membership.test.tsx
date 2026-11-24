import { fireEvent, render, screen } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import '@/i18n';
import type { Schemas } from '@/lib/api';
import { CancelMembership } from './cancel-membership';

const membership: Schemas['MyMembership'] = {
  membershipType: 'General membership',
  monthlyPriceOre: 29_900,
  status: 'Active',
  memberSince: '2026-11-23',
  paidUntil: '2026-12-22',
  cancelledAt: null,
  cancellationEffectiveOn: null,
  canUndoCancellation: false,
};

test('cancelling takes one confirmation and no other step', async () => {
  const onCancel = vi.fn().mockResolvedValue('ok');
  render(
    <CancelMembership
      membership={membership}
      onCancel={onCancel}
      onUndo={vi.fn()}
    />,
  );

  fireEvent.click(screen.getByRole('button', { name: 'Cancel membership' }));
  expect(onCancel).not.toHaveBeenCalled();
  fireEvent.click(
    screen.getByRole('button', { name: 'Yes, cancel my membership' }),
  );

  expect(onCancel).toHaveBeenCalledOnce();
});

test('going back does not cancel', async () => {
  const onCancel = vi.fn().mockResolvedValue('ok');
  render(
    <CancelMembership
      membership={membership}
      onCancel={onCancel}
      onUndo={vi.fn()}
    />,
  );

  fireEvent.click(screen.getByRole('button', { name: 'Cancel membership' }));
  fireEvent.click(screen.getByRole('button', { name: 'Go back' }));

  expect(onCancel).not.toHaveBeenCalled();
  expect(
    screen.getByRole('button', { name: 'Cancel membership' }),
  ).toBeInTheDocument();
});

test('a failed cancellation says so', async () => {
  render(
    <CancelMembership
      membership={membership}
      onCancel={vi.fn().mockResolvedValue('failed')}
      onUndo={vi.fn()}
    />,
  );

  fireEvent.click(screen.getByRole('button', { name: 'Cancel membership' }));
  fireEvent.click(
    screen.getByRole('button', { name: 'Yes, cancel my membership' }),
  );

  expect(await screen.findByRole('alert')).toHaveTextContent(
    'could not be cancelled',
  );
});

test('a pending cancellation shows the last day of access and no cancel button', () => {
  render(
    <CancelMembership
      membership={{
        ...membership,
        cancelledAt: '2027-03-10T09:30:00Z',
        cancellationEffectiveOn: '2027-04-30',
      }}
      onCancel={vi.fn()}
      onUndo={vi.fn()}
    />,
  );

  expect(screen.getByRole('status')).toHaveTextContent(
    'You have access until 30 Apr 2027',
  );
  expect(
    screen.queryByRole('button', { name: 'Cancel membership' }),
  ).not.toBeInTheDocument();
});

const pending: Schemas['MyMembership'] = {
  ...membership,
  cancelledAt: '2027-03-10T09:30:00Z',
  cancellationEffectiveOn: '2027-04-30',
  canUndoCancellation: true,
};

test('a pending cancellation can be undone with one click', async () => {
  const onUndo = vi.fn().mockResolvedValue('ok');
  render(
    <CancelMembership
      membership={pending}
      onCancel={vi.fn()}
      onUndo={onUndo}
    />,
  );

  fireEvent.click(screen.getByRole('button', { name: 'Undo cancellation' }));

  expect(onUndo).toHaveBeenCalledOnce();
});

test('a failed undo says so', async () => {
  render(
    <CancelMembership
      membership={pending}
      onCancel={vi.fn()}
      onUndo={vi.fn().mockResolvedValue('failed')}
    />,
  );

  fireEvent.click(screen.getByRole('button', { name: 'Undo cancellation' }));

  expect(await screen.findByRole('alert')).toHaveTextContent(
    'could not be undone',
  );
});

test('after the effective date there is no undo button', () => {
  render(
    <CancelMembership
      membership={{ ...pending, canUndoCancellation: false }}
      onCancel={vi.fn()}
      onUndo={vi.fn()}
    />,
  );

  expect(screen.getByRole('status')).toHaveTextContent(
    'You have access until 30 Apr 2027',
  );
  expect(
    screen.queryByRole('button', { name: 'Undo cancellation' }),
  ).not.toBeInTheDocument();
});

test('without a cancellation there is no undo button', () => {
  render(
    <CancelMembership
      membership={membership}
      onCancel={vi.fn()}
      onUndo={vi.fn()}
    />,
  );

  expect(
    screen.queryByRole('button', { name: 'Undo cancellation' }),
  ).not.toBeInTheDocument();
});
