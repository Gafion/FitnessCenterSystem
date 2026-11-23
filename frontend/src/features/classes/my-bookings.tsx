import { useTranslation } from 'react-i18next';
import type { Schemas } from '@/lib/api';
import { formatDateTime, formatTime } from './class-time';

/** The account's booked classes that haven't started, soonest first, in Danish time (D-022). */
export function MyBookings({
  bookings,
}: {
  bookings: readonly Schemas['MyBooking'][];
}) {
  const { t } = useTranslation();
  return bookings.length === 0 ? (
    <p className="text-muted-foreground">{t('myBookings.none')}</p>
  ) : (
    <ul className="flex flex-col gap-2">
      {bookings.map((booking) => (
        <li key={booking.classId} className="flex flex-col">
          <span className="font-medium">{booking.className}</span>
          <span>
            {t('myBookings.when', {
              start: formatDateTime(booking.startsAt),
              end: formatTime(booking.endsAt),
            })}{' '}
            <span className="text-muted-foreground">
              {t('classes.instructor', { name: booking.instructorName })}
            </span>
          </span>
        </li>
      ))}
    </ul>
  );
}
