import type { Schemas } from '@/lib/api';

/** A synthetic class: Tuesday 24 November 2026, 18:00–18:45 in Copenhagen (UTC+1). */
export function classEntry(
  overrides: Partial<Schemas['ScheduleEntry']> = {},
): Schemas['ScheduleEntry'] {
  return {
    classId: '0199a3c0-0000-7000-8000-000000000c01',
    className: 'Spinning',
    instructorName: 'Test Instructor',
    startsAt: '2026-11-24T17:00:00Z',
    endsAt: '2026-11-24T17:45:00Z',
    capacity: 12,
    spotsLeft: 5,
    bookedByMe: false,
    cancelUntil: '2026-11-24T05:00:00Z',
    afterMyMembershipEnds: false,
    myWaitlistPlace: null,
    ...overrides,
  };
}

/** Well before the cut-off of {@link classEntry}. */
export const beforeCutOff = new Date('2026-11-23T12:00:00Z');

/** After the cut-off of {@link classEntry}, before its start. */
export const afterCutOff = new Date('2026-11-24T10:00:00Z');
