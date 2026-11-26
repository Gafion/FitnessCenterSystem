import { CalendarX2Icon, UsersIcon } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import {
  formatDay,
  formatTime,
  groupByDay,
} from '@/features/classes/class-time';
import type { Schemas } from '@/lib/api';

/**
 * The instructor's upcoming classes, one section per Copenhagen day, each with its
 * capacity and the booked members' names (D-049, D-060). Names only: the API sends
 * nothing else about the members (GDPR minimum).
 */
export function MyClasses({
  classes,
}: {
  classes: readonly Schemas['MyClass'][];
}) {
  const { t } = useTranslation();
  if (classes.length === 0) {
    return (
      <p className="flex flex-col items-center gap-3 rounded-lg border border-dashed border-border-strong px-6 py-12 text-center text-muted-foreground">
        <CalendarX2Icon aria-hidden="true" className="size-8 text-subtle" />
        {t('myClasses.empty')}
      </p>
    );
  }

  return (
    <div className="flex flex-col gap-8">
      {groupByDay(classes).map(({ day, entries }) => (
        <section
          key={day}
          aria-labelledby={`day-${day}`}
          className="flex flex-col gap-2"
        >
          <h2
            id={`day-${day}`}
            className="text-eyebrow font-medium text-subtle uppercase"
          >
            {formatDay(entries[0].startsAt)}
          </h2>
          <ul className="divide-y divide-border overflow-hidden rounded-lg border border-border bg-card">
            {entries.map((entry) => (
              <TaughtClass key={entry.classId} entry={entry} />
            ))}
          </ul>
        </section>
      ))}
    </div>
  );
}

function TaughtClass({ entry }: { entry: Schemas['MyClass'] }) {
  const { t } = useTranslation();
  const headingId = `class-${entry.classId}`;
  return (
    <li aria-labelledby={headingId} className="flex flex-col gap-3 p-4">
      <div className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
        <h3 id={headingId} className="font-medium">
          {entry.className}
        </h3>
        <span className="text-sm text-muted-foreground tabular-nums">
          {t('myClasses.when', {
            start: formatTime(entry.startsAt),
            end: formatTime(entry.endsAt),
          })}
        </span>
      </div>
      <p className="flex items-center gap-2 text-sm text-muted-foreground">
        <UsersIcon aria-hidden="true" className="size-4 text-subtle" />
        {t('myClasses.booked', {
          booked: entry.bookedNames.length,
          count: entry.capacity,
        })}
      </p>
      {entry.bookedNames.length === 0 ? (
        <p className="text-sm text-subtle">{t('myClasses.noBookings')}</p>
      ) : (
        <ul
          aria-label={t('myClasses.bookedNames', {
            className: entry.className,
          })}
          className="flex flex-col gap-1 text-sm"
        >
          {entry.bookedNames.map((name, index) => (
            // Names are not unique ("Test M." twice), and the list has no IDs by design.
            // biome-ignore lint/suspicious/noArrayIndexKey: the list is static and never reordered
            <li key={index}>{name}</li>
          ))}
        </ul>
      )}
    </li>
  );
}
