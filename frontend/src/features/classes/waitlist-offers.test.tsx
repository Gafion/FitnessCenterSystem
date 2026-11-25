import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import '@/i18n';
import type { Schemas } from '@/lib/api';
import { WaitlistOffers } from './waitlist-offers';

// The account page's notice of a spot held from the waitlist (D-067, FIT-80).

const offer: Schemas['MyOffer'] = {
  classId: '0199a3c0-0000-7000-8000-000000000c01',
  className: 'Spinning',
  instructorName: 'Test Instructor',
  // 18:00–18:45 on 24 November 2026 in Copenhagen; held until 14:00 the day before.
  startsAt: '2026-11-24T17:00:00Z',
  endsAt: '2026-11-24T17:45:00Z',
  expiresAt: '2026-11-23T13:00:00Z',
};

test('an open offer shows the class, the expiry and Accept and Decline', async () => {
  const onAccept = vi.fn().mockResolvedValue(undefined);
  const onDecline = vi.fn().mockResolvedValue(undefined);
  render(
    <WaitlistOffers
      offers={[offer]}
      onAccept={onAccept}
      onDecline={onDecline}
    />,
  );

  expect(
    screen.getByRole('heading', { name: 'A spot is free for you' }),
  ).toBeInTheDocument();
  expect(screen.getByText(/Spinning, .*18:00–18:45/)).toHaveTextContent(
    /Accept by .*14:00, or it goes to the next person on the waitlist\./,
  );
  fireEvent.click(screen.getByRole('button', { name: 'Accept spot' }));
  // The buttons wait while the answer is sent.
  const decline = screen.getByRole('button', { name: 'Decline' });
  await waitFor(() => expect(decline).toBeEnabled());
  fireEvent.click(decline);

  expect(onAccept).toHaveBeenCalledWith(offer.classId);
  expect(onDecline).toHaveBeenCalledWith(offer.classId);
});

test('without offers there is no notice', () => {
  const { container } = render(
    <WaitlistOffers offers={[]} onAccept={vi.fn()} onDecline={vi.fn()} />,
  );

  expect(container).toBeEmptyDOMElement();
});

test('an expired offer says so', async () => {
  render(
    <WaitlistOffers
      offers={[offer]}
      onAccept={() => Promise.resolve('offer-expired')}
      onDecline={vi.fn()}
    />,
  );

  fireEvent.click(screen.getByRole('button', { name: 'Accept spot' }));

  expect(await screen.findByRole('alert')).toHaveTextContent(
    'The offer has run out, so the spot went to the next person.',
  );
});
