import { useTranslation } from 'react-i18next';
import type { Schemas } from '@/lib/api';
import { type BookingResult, ClassCard } from './class-card';
import { formatDay, groupByDay } from './class-time';

type Props = {
  entries: Schemas['ScheduleEntry'][];
  now: Date;
  onBook: (classId: string) => Promise<BookingResult>;
  onCancel: (classId: string) => Promise<BookingResult>;
};

/** The classes of the booking window, one section per Copenhagen day (D-039). */
export function Schedule({ entries, now, onBook, onCancel }: Props) {
  const { t } = useTranslation();
  if (entries.length === 0) {
    return <p className="text-muted-foreground">{t('classes.empty')}</p>;
  }

  return (
    <div className="flex flex-col gap-8">
      {groupByDay(entries).map(({ day, entries: dayEntries }) => (
        <section
          key={day}
          aria-labelledby={`day-${day}`}
          className="flex flex-col gap-3"
        >
          <h2 id={`day-${day}`} className="text-xl font-semibold">
            {formatDay(dayEntries[0].startsAt)}
          </h2>
          {dayEntries.map((entry) => (
            <ClassCard
              key={entry.classId}
              entry={entry}
              now={now}
              onBook={() => onBook(entry.classId)}
              onCancel={() => onCancel(entry.classId)}
            />
          ))}
        </section>
      ))}
    </div>
  );
}
