import { fireEvent, render, screen } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import '@/i18n';
import { type BookingResult, ClassCard } from './class-card';
import { beforeCutOff, classEntry } from './test-entries';

// A spot held for the member from the waitlist (D-051, D-067, FIT-80).

const noop = () => Promise.resolve(undefined);

/** 14:00 in Copenhagen on 23 November 2026. */
const expiresAt = '2026-11-23T13:00:00Z';

function renderCard(
  entry: Parameters<typeof classEntry>[0],
  handlers: {
    onAcceptOffer?: () => Promise<BookingResult>;
    onDeclineOffer?: () => Promise<BookingResult>;
  } = {},
) {
  render(
    <ClassCard
      entry={classEntry(entry)}
      now={beforeCutOff}
      onBook={noop}
      onCancel={noop}
      onJoinWaitlist={noop}
      onLeaveWaitlist={noop}
      onAcceptOffer={handlers.onAcceptOffer ?? noop}
      onDeclineOffer={handlers.onDeclineOffer ?? noop}
    />,
  );
}

test('a held spot shows the expiry and accepts', () => {
  const onAcceptOffer = vi.fn().mockResolvedValue(undefined);
  renderCard({ spotsLeft: 0, myOfferExpiresAt: expiresAt }, { onAcceptOffer });

  expect(screen.getByText('A spot is held for you.')).toBeInTheDocument();
  expect(
    screen.getByText(/Accept by .*14:00, or it goes to the next person\./),
  ).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'Accept spot' }));

  expect(onAcceptOffer).toHaveBeenCalledOnce();
});

test('a held spot can be declined', () => {
  const onDeclineOffer = vi.fn().mockResolvedValue(undefined);
  renderCard({ spotsLeft: 0, myOfferExpiresAt: expiresAt }, { onDeclineOffer });

  fireEvent.click(screen.getByRole('button', { name: 'Decline' }));

  expect(onDeclineOffer).toHaveBeenCalledOnce();
});

test('a held spot offers neither the waitlist nor Book, and does not say full', () => {
  renderCard({ spotsLeft: 0, myOfferExpiresAt: expiresAt });

  expect(
    screen.queryByRole('button', { name: 'Join waitlist' }),
  ).not.toBeInTheDocument();
  expect(
    screen.queryByRole('button', { name: 'Book' }),
  ).not.toBeInTheDocument();
  expect(screen.queryByText('This class is full.')).not.toBeInTheDocument();
});

test('an offer that ran out says so', async () => {
  renderCard(
    { spotsLeft: 0, myOfferExpiresAt: expiresAt },
    { onAcceptOffer: () => Promise.resolve('offer-expired') },
  );

  fireEvent.click(screen.getByRole('button', { name: 'Accept spot' }));

  expect(await screen.findByRole('alert')).toHaveTextContent(
    'The offer has run out, so the spot went to the next person.',
  );
});

test('without an offer there is no Accept button', () => {
  renderCard({ spotsLeft: 0 });

  expect(
    screen.queryByRole('button', { name: 'Accept spot' }),
  ).not.toBeInTheDocument();
});
