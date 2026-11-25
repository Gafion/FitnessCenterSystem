import { BanIcon, CheckIcon, HourglassIcon } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import type { Schemas } from '@/lib/api';
import { formatDateTime, formatTime } from './class-time';

/**
 * What a book or cancel attempt came to: undefined when it worked, otherwise the
 * problem `type` the API answered with, e.g. "class-full" (D-039).
 */
export type BookingResult = string | undefined;

type Props = {
  entry: Schemas['ScheduleEntry'];
  /** The moment the page treats as now; decides whether cancelling is still open. */
  now: Date;
  onBook: () => Promise<BookingResult>;
  onCancel: () => Promise<BookingResult>;
  /** Joins the waitlist of a full class (D-051). Without it, the card offers no waitlist. */
  onJoinWaitlist?: () => Promise<BookingResult>;
  onLeaveWaitlist?: () => Promise<BookingResult>;
};

export function ClassCard({
  entry,
  now,
  onBook,
  onCancel,
  onJoinWaitlist,
  onLeaveWaitlist,
}: Props) {
  const { t } = useTranslation();
  const [error, setError] = useState<string>();
  const [busy, setBusy] = useState(false);
  const titleId = `class-${entry.classId}`;
  const canCancel = now < new Date(entry.cancelUntil);

  async function run(action: () => Promise<BookingResult>) {
    setBusy(true);
    setError(await action());
    setBusy(false);
  }

  const full = entry.spotsLeft === 0;
  const bookable = !full && !entry.afterMyMembershipEnds;
  // D-051: the member's place in the queue, counting from 1; absent when not waiting.
  const waitlistPlace = entry.myWaitlistPlace ?? undefined;
  const waiting = waitlistPlace !== undefined;
  const canJoinWaitlist =
    full && !entry.bookedByMe && !waiting && !entry.afterMyMembershipEnds;

  return (
    <article
      aria-labelledby={titleId}
      className="flex flex-col gap-3 p-4 sm:flex-row sm:items-start sm:gap-6 sm:p-5"
    >
      <p className="text-lg font-semibold tabular-nums sm:w-32 sm:shrink-0 sm:pt-0.5">
        {t('classes.time', {
          start: formatTime(entry.startsAt),
          end: formatTime(entry.endsAt),
        })}
      </p>
      <div className="flex min-w-0 flex-1 flex-col gap-1.5">
        <div className="flex flex-wrap items-center gap-2">
          <h3 id={titleId} className="text-base font-semibold">
            {entry.className}
          </h3>
          <Badge variant={full ? 'danger' : 'neutral'}>
            {t('classes.spotsLeft', { count: entry.spotsLeft })}
          </Badge>
        </div>
        <p className="text-sm text-muted-foreground">
          {t('classes.instructor', { name: entry.instructorName })}
        </p>
        {entry.bookedByMe && (
          <div className="flex flex-col items-start gap-1">
            <Badge variant="success">
              <CheckIcon aria-hidden="true" className="size-3.5" />
              {t('classes.booked')}
            </Badge>
            <p className="text-sm text-subtle">
              {t(canCancel ? 'classes.cancelUntil' : 'classes.cancelClosed', {
                time: formatDateTime(entry.cancelUntil),
              })}
            </p>
          </div>
        )}
        {waiting && (
          <div className="flex flex-col items-start gap-1">
            <Badge variant="default">
              <HourglassIcon aria-hidden="true" className="size-3.5" />
              {t('classes.waitlisted')}
            </Badge>
            <p className="text-sm text-subtle">
              {t('classes.waitlistPlace', { place: waitlistPlace })}
            </p>
          </div>
        )}
        {!entry.bookedByMe && !waiting && full && (
          <p className="text-sm text-muted-foreground">{t('classes.full')}</p>
        )}
        {/* D-055: a cancelled membership can't book classes after its last day. */}
        {!entry.bookedByMe && entry.afterMyMembershipEnds && (
          <p className="text-sm text-muted-foreground">
            {t('classes.afterMembershipEnds')}
          </p>
        )}
        {error && (
          <p
            role="alert"
            className="flex gap-1.5 text-sm font-medium text-destructive"
          >
            {t(`classes.errors.${error}`, {
              defaultValue: t('errors.unexpected'),
            })}
          </p>
        )}
      </div>
      {entry.bookedByMe ? (
        canCancel && (
          <Button
            variant="outline"
            className="w-full sm:w-36"
            disabled={busy}
            onClick={() => run(onCancel)}
          >
            {t('classes.cancel')}
          </Button>
        )
      ) : (
        <div className="flex flex-col gap-2 sm:w-36">
          {canJoinWaitlist && onJoinWaitlist && (
            <Button
              className="w-full"
              disabled={busy}
              onClick={() => run(onJoinWaitlist)}
            >
              {t('classes.joinWaitlist')}
            </Button>
          )}
          {waiting && onLeaveWaitlist && (
            <Button
              variant="outline"
              className="w-full"
              disabled={busy}
              onClick={() => run(onLeaveWaitlist)}
            >
              {t('classes.leaveWaitlist')}
            </Button>
          )}
          {/* D-051, FIT-80: a full class offers only the waitlist, no Book button. */}
          {!full && (
            <Button
              // A class that can't be booked gets a dashed, flat button with a
              // "no" sign, so it never reads as a working Book button.
              variant={waiting ? 'outline' : undefined}
              className="w-full data-disabled:border-dashed data-disabled:border-border-strong"
              disabled={busy || !bookable}
              onClick={() => run(onBook)}
            >
              {!bookable && <BanIcon aria-hidden="true" />}
              {t('classes.book')}
            </Button>
          )}
        </div>
      )}
    </article>
  );
}
