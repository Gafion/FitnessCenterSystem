import { fireEvent, render, screen } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import '@/i18n';
import { type BookingResult, ClassCard } from './class-card';
import { beforeCutOff, classEntry } from './test-entries';

// The waitlist of a full class (D-051, FIT-79).

const noop = () => Promise.resolve(undefined);

function renderCard(
  entry: Parameters<typeof classEntry>[0],
  handlers: {
    onJoinWaitlist?: () => Promise<BookingResult>;
    onLeaveWaitlist?: () => Promise<BookingResult>;
  } = {},
) {
  render(
    <ClassCard
      entry={classEntry(entry)}
      now={beforeCutOff}
      onBook={noop}
      onCancel={noop}
      onJoinWaitlist={handlers.onJoinWaitlist ?? noop}
      onLeaveWaitlist={handlers.onLeaveWaitlist ?? noop}
    />,
  );
}

test('a full class offers to join the waitlist', () => {
  const onJoinWaitlist = vi.fn().mockResolvedValue(undefined);
  renderCard({ spotsLeft: 0 }, { onJoinWaitlist });

  fireEvent.click(screen.getByRole('button', { name: 'Join waitlist' }));

  expect(onJoinWaitlist).toHaveBeenCalledOnce();
  expect(screen.getByText('This class is full.')).toBeInTheDocument();
});

test('a class with free spots has no waitlist button', () => {
  renderCard({ spotsLeft: 3 });

  expect(
    screen.queryByRole('button', { name: 'Join waitlist' }),
  ).not.toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Book' })).toBeEnabled();
});

test('a waiting member sees their place and can leave', () => {
  const onLeaveWaitlist = vi.fn().mockResolvedValue(undefined);
  renderCard({ spotsLeft: 0, myWaitlistPlace: 3 }, { onLeaveWaitlist });

  expect(screen.getByText("You're on the waitlist.")).toBeInTheDocument();
  expect(screen.getByText("You're number 3 in the queue.")).toBeInTheDocument();
  expect(
    screen.queryByRole('button', { name: 'Join waitlist' }),
  ).not.toBeInTheDocument();

  fireEvent.click(screen.getByRole('button', { name: 'Leave waitlist' }));

  expect(onLeaveWaitlist).toHaveBeenCalledOnce();
});

test('a booked member is not offered the waitlist', () => {
  renderCard({ spotsLeft: 0, bookedByMe: true });

  expect(
    screen.queryByRole('button', { name: 'Join waitlist' }),
  ).not.toBeInTheDocument();
});

test('a class after the membership ends offers no waitlist', () => {
  renderCard({ spotsLeft: 0, afterMyMembershipEnds: true });

  expect(
    screen.queryByRole('button', { name: 'Join waitlist' }),
  ).not.toBeInTheDocument();
});

test.each([
  ['already-waitlisted', "You're already on the waitlist for this class."],
  ['class-not-full', 'This class has free spots, so book it instead.'],
])('a refused join shows "%s"', async (type, message) => {
  renderCard({ spotsLeft: 0 }, { onJoinWaitlist: () => Promise.resolve(type) });

  fireEvent.click(screen.getByRole('button', { name: 'Join waitlist' }));

  expect(await screen.findByRole('alert')).toHaveTextContent(message);
});
