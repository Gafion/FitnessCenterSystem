import { CalendarX2Icon } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import type { Schemas } from '@/lib/api';
import { type BookingResult, ClassCard } from './class-card';
import { formatDay, groupByDay } from './class-time';

type Props = {
  entries: Schemas['ScheduleEntry'][];
  now: Date;
  onBook: (classId: string) => Promise<BookingResult>;
  onCancel: (classId: string) => Promise<BookingResult>;
  onJoinWaitlist?: (classId: string) => Promise<BookingResult>;
  onLeaveWaitlist?: (classId: string) => Promise<BookingResult>;
};

/** The classes of the booking window, one section per Copenhagen day (D-039). */
export function Schedule({
  entries,
  now,
  onBook,
  onCancel,
  onJoinWaitlist,
  onLeaveWaitlist,
}: Props) {
  const { t } = useTranslation();
  if (entries.length === 0) {
    return (
      <p className="flex flex-col items-center gap-3 rounded-lg border border-dashed border-border-strong px-6 py-12 text-center text-muted-foreground">
        <CalendarX2Icon aria-hidden="true" className="size-8 text-subtle" />
        {t('classes.empty')}
      </p>
    );
  }

  return (
    <div className="flex flex-col gap-8">
      {groupByDay(entries).map(({ day, entries: dayEntries }) => (
        <section
          key={day}
          aria-labelledby={`day-${day}`}
          className="flex flex-col gap-2"
        >
          <h2
            id={`day-${day}`}
            className="text-eyebrow font-medium text-subtle uppercase"
          >
            {formatDay(dayEntries[0].startsAt)}
          </h2>
          <div className="divide-y divide-border overflow-hidden rounded-lg border border-border bg-card">
            {dayEntries.map((entry) => (
              <ClassCard
                key={entry.classId}
                entry={entry}
                now={now}
                onBook={() => onBook(entry.classId)}
                onCancel={() => onCancel(entry.classId)}
                onJoinWaitlist={
                  onJoinWaitlist && (() => onJoinWaitlist(entry.classId))
                }
                onLeaveWaitlist={
                  onLeaveWaitlist && (() => onLeaveWaitlist(entry.classId))
                }
              />
            ))}
          </div>
        </section>
      ))}
    </div>
  );
}
