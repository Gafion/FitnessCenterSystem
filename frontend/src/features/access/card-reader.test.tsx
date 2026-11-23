import { fireEvent, render, screen } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import '@/i18n';
import { CardReader } from './card-reader';

// Synthetic UIDs only (CLAUDE.md).
const myCard = 'SIM-04A1B2C3D4E5F6';
const at = '2027-01-12T06:30:00Z';
const welcome = { outcome: 'welcome' as const, firstName: 'Test', at };
const unknown = { outcome: 'unknown-card' as const, firstName: null, at };

test("shows the member's card and swipes it", async () => {
  const onSwipeMyCard = vi.fn().mockResolvedValue(welcome);
  render(
    <CardReader
      cardUid={myCard}
      onSwipeMyCard={onSwipeMyCard}
      onSwipe={vi.fn()}
    />,
  );

  expect(screen.getByText(myCard)).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'Swipe my card' }));

  expect(await screen.findByText('Welcome, Test!')).toBeInTheDocument();
  expect(onSwipeMyCard).toHaveBeenCalledOnce();
});

test('swipes a typed UID and shows an unknown card', async () => {
  const onSwipe = vi.fn().mockResolvedValue(unknown);
  render(
    <CardReader cardUid={myCard} onSwipeMyCard={vi.fn()} onSwipe={onSwipe} />,
  );

  fireEvent.change(screen.getByLabelText('Card UID'), {
    target: { value: 'SIM-0000000000000' },
  });
  fireEvent.click(screen.getByRole('button', { name: 'Swipe' }));

  expect(await screen.findByText('Unknown card')).toBeInTheDocument();
  expect(onSwipe).toHaveBeenCalledWith('SIM-0000000000000');
});

test('without a card, only a typed UID can be swiped', () => {
  render(
    <CardReader cardUid={null} onSwipeMyCard={vi.fn()} onSwipe={vi.fn()} />,
  );

  expect(screen.getByText(/You have no access card/)).toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Swipe my card' })).toBeDisabled();
  expect(screen.getByRole('button', { name: 'Swipe' })).toBeEnabled();
});

test('says so when the reader gets no answer', async () => {
  render(
    <CardReader
      cardUid={myCard}
      onSwipeMyCard={vi.fn().mockResolvedValue(undefined)}
      onSwipe={vi.fn()}
    />,
  );

  fireEvent.click(screen.getByRole('button', { name: 'Swipe my card' }));

  expect(await screen.findByRole('alert')).toHaveTextContent(
    "The reader didn't answer. Try again.",
  );
});
