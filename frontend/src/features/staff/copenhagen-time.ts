// Staff enter a class's start as a date and a time on the center's wall clock in
// Europe/Copenhagen (CLAUDE.md "Time"), whatever zone the device is in. The API takes
// an instant, so these helpers convert between the two with Intl alone.

const timeZone = 'Europe/Copenhagen';

const partsFormat = new Intl.DateTimeFormat('en-CA', {
  timeZone,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
});

/** A Copenhagen wall-clock time: `date` as YYYY-MM-DD and `time` as HH:mm. */
export type LocalDateTime = { date: string; time: string };

function wallClock(instant: Date): LocalDateTime {
  const parts = Object.fromEntries(
    partsFormat.formatToParts(instant).map((part) => [part.type, part.value]),
  );
  return {
    date: `${parts.year}-${parts.month}-${parts.day}`,
    time: `${parts.hour}:${parts.minute}`,
  };
}

/** The Copenhagen date and time of an instant, for the form's date and time fields. */
export function toCopenhagen(instant: string): LocalDateTime {
  return wallClock(new Date(instant));
}

function asUtcMillis({ date, time }: LocalDateTime): number {
  return Date.parse(`${date}T${time}:00Z`);
}

/**
 * The instant, as an ISO string in UTC, of a Copenhagen date and time. Null when the
 * fields are not a valid date and time. A time the spring change skips (02:30 on the
 * last Sunday of March) moves forward an hour; an hour the autumn change repeats takes
 * the first of the two, summer time.
 */
export function fromCopenhagen(local: LocalDateTime): string | null {
  if (
    !/^\d{4}-\d{2}-\d{2}$/.test(local.date) ||
    !/^\d{2}:\d{2}$/.test(local.time)
  ) {
    return null;
  }
  const wanted = asUtcMillis(local);
  if (Number.isNaN(wanted)) {
    return null;
  }
  // Copenhagen is UTC+1 or UTC+2. Try the summer offset first so a repeated autumn
  // hour gives the earlier instant.
  for (const offsetHours of [2, 1]) {
    const candidate = new Date(wanted - offsetHours * 3_600_000);
    if (asUtcMillis(wallClock(candidate)) === wanted) {
      return candidate.toISOString();
    }
  }
  // In the skipped spring hour no instant matches; the winter offset lands an hour later.
  return new Date(wanted - 3_600_000).toISOString();
}
