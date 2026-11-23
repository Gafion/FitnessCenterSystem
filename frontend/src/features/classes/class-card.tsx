import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
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
};

export function ClassCard({ entry, now, onBook, onCancel }: Props) {
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

  return (
    <Card aria-labelledby={titleId}>
      <CardHeader>
        <CardTitle id={titleId}>{entry.className}</CardTitle>
        <CardDescription>
          {t('classes.instructor', { name: entry.instructorName })}
        </CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-1 text-sm">
        <p>
          {t('classes.time', {
            start: formatTime(entry.startsAt),
            end: formatTime(entry.endsAt),
          })}
        </p>
        <p>{t('classes.spotsLeft', { count: entry.spotsLeft })}</p>
        {entry.bookedByMe && (
          <>
            <p className="font-medium">{t('classes.booked')}</p>
            <p className="text-muted-foreground">
              {t(canCancel ? 'classes.cancelUntil' : 'classes.cancelClosed', {
                time: formatDateTime(entry.cancelUntil),
              })}
            </p>
          </>
        )}
        {!entry.bookedByMe && entry.spotsLeft === 0 && (
          <p className="text-muted-foreground">{t('classes.full')}</p>
        )}
        {error && (
          <p role="alert" className="text-destructive">
            {t(`classes.errors.${error}`, {
              defaultValue: t('errors.unexpected'),
            })}
          </p>
        )}
      </CardContent>
      <CardFooter>
        {entry.bookedByMe ? (
          canCancel && (
            <Button
              variant="outline"
              disabled={busy}
              onClick={() => run(onCancel)}
            >
              {t('classes.cancel')}
            </Button>
          )
        ) : (
          <Button
            disabled={busy || entry.spotsLeft === 0}
            onClick={() => run(onBook)}
          >
            {t('classes.book')}
          </Button>
        )}
      </CardFooter>
    </Card>
  );
}
