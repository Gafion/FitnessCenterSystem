import { CalendarX2Icon, UserIcon, UsersIcon } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  formatDateTime,
  formatDay,
  formatTime,
  groupByDay,
} from '@/features/classes/class-time';
import type { Schemas } from '@/lib/api';
import { ClassForm, type ClassRequest, type SaveResult } from './class-form';

type StaffClass = Schemas['StaffClass'];

type Props = {
  classes: readonly StaffClass[];
  classTypes: readonly Schemas['StaffClassType'][];
  instructors: readonly Schemas['StaffMember'][];
  /** The time the page loaded: a class that has started can't be changed (D-068). */
  now: Date;
  onChange: (classId: string, request: ClassRequest) => Promise<SaveResult>;
  /** "ok", or the API's problem code ("class-started", "unexpected", …). */
  onCancel: (classId: string) => Promise<string>;
};

/**
 * The center's classes, one section per Copenhagen day, cancelled ones included, with
 * the bookings and waitlist on each. A manager or admin edits, moves or cancels a class
 * that hasn't started (D-062, D-068).
 */
export function StaffClassList({ classes, ...props }: Props) {
  const { t } = useTranslation();
  if (classes.length === 0) {
    return (
      <p className="flex flex-col items-center gap-3 rounded-lg border border-dashed border-border-strong px-6 py-12 text-center text-muted-foreground">
        <CalendarX2Icon aria-hidden="true" className="size-8 text-subtle" />
        {t('staffClasses.empty')}
      </p>
    );
  }

  return (
    <div className="flex flex-col gap-8">
      {groupByDay(classes).map(({ day, entries }) => (
        <section
          key={day}
          aria-labelledby={`staff-day-${day}`}
          className="flex flex-col gap-2"
        >
          <h3
            id={`staff-day-${day}`}
            className="text-eyebrow font-medium text-subtle uppercase"
          >
            {formatDay(entries[0].startsAt)}
          </h3>
          <ul className="divide-y divide-border overflow-hidden rounded-lg border border-border bg-card">
            {entries.map((entry) => (
              <ClassRow key={entry.classId} entry={entry} {...props} />
            ))}
          </ul>
        </section>
      ))}
    </div>
  );
}

function ClassRow({
  entry,
  classTypes,
  instructors,
  now,
  onChange,
  onCancel,
}: Omit<Props, 'classes'> & { entry: StaffClass }) {
  const { t } = useTranslation();
  const [mode, setMode] = useState<'view' | 'edit' | 'confirm-cancel'>('view');
  const [busy, setBusy] = useState(false);
  const [problem, setProblem] = useState<string | null>(null);
  const headingId = `staff-class-${entry.classId}`;
  const cancelled = entry.status === 'Cancelled';
  const started = new Date(entry.startsAt).getTime() <= now.getTime();

  async function cancel() {
    setBusy(true);
    setProblem(null);
    const result = await onCancel(entry.classId);
    setBusy(false);
    if (result === 'ok') {
      setMode('view');
    } else {
      setProblem(result);
    }
  }

  return (
    <li aria-labelledby={headingId} className="flex flex-col gap-3 p-4">
      <div className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
        <h4
          id={headingId}
          className="flex items-center gap-2 font-medium data-cancelled:text-muted-foreground data-cancelled:line-through"
          data-cancelled={cancelled ? '' : undefined}
        >
          {entry.className}
        </h4>
        <span className="text-sm text-muted-foreground tabular-nums">
          {t('myClasses.when', {
            start: formatTime(entry.startsAt),
            end: formatTime(entry.endsAt),
          })}
        </span>
      </div>
      <div className="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-muted-foreground">
        {cancelled && (
          <Badge variant="danger">{t('staffClasses.cancelled')}</Badge>
        )}
        <span className="flex items-center gap-2">
          <UserIcon aria-hidden="true" className="size-4 text-subtle" />
          {entry.instructorName}
        </span>
        {!cancelled && (
          <span className="flex items-center gap-2">
            <UsersIcon aria-hidden="true" className="size-4 text-subtle" />
            {t('staffClasses.booked', {
              booked: entry.bookedCount,
              count: entry.capacity,
            })}
          </span>
        )}
        {!cancelled && entry.waitlistCount > 0 && (
          <span>
            {t('staffClasses.waitlist', { count: entry.waitlistCount })}
          </span>
        )}
      </div>

      {mode === 'edit' && (
        <div className="rounded-md border border-border bg-muted/40 p-4">
          <ClassForm
            classTypes={classTypes}
            instructors={instructors}
            editing={entry}
            onSubmit={(request) => onChange(entry.classId, request)}
            onClose={() => setMode('view')}
          />
        </div>
      )}

      {mode === 'confirm-cancel' && (
        <div className="flex flex-col gap-3 rounded-md border border-destructive/30 bg-destructive/5 p-4">
          <p>
            {t('staffClasses.cancelConfirm.question', {
              className: entry.className,
              when: formatDateTime(entry.startsAt),
            })}
          </p>
          <p className="text-sm text-muted-foreground">
            {entry.bookedCount > 0
              ? t('staffClasses.cancelConfirm.emails', {
                  count: entry.bookedCount,
                })
              : t('staffClasses.cancelConfirm.noBookings')}
            {entry.waitlistCount > 0 &&
              ` ${t('staffClasses.cancelConfirm.waitlist')}`}
          </p>
          <div className="flex flex-col gap-2 sm:flex-row sm:flex-wrap">
            <Button variant="destructive" disabled={busy} onClick={cancel}>
              {t('staffClasses.cancelConfirm.confirm')}
            </Button>
            <Button
              variant="outline"
              disabled={busy}
              onClick={() => {
                setMode('view');
                setProblem(null);
              }}
            >
              {t('staffClasses.cancelConfirm.back')}
            </Button>
          </div>
        </div>
      )}

      {problem && (
        <p role="alert" className="text-sm text-destructive">
          {t(`staffClasses.errors.${knownProblem(problem)}`)}
        </p>
      )}

      {mode === 'view' && !cancelled && !started && (
        <div className="flex flex-wrap gap-2">
          <Button
            variant="outline"
            size="sm"
            aria-describedby={headingId}
            onClick={() => setMode('edit')}
          >
            {t('staffClasses.edit')}
          </Button>
          <Button
            variant="ghost"
            size="sm"
            aria-describedby={headingId}
            className="text-muted-foreground hover:not-data-disabled:text-destructive"
            onClick={() => {
              setProblem(null);
              setMode('confirm-cancel');
            }}
          >
            {t('staffClasses.cancelClass')}
          </Button>
        </div>
      )}
      {mode === 'view' && !cancelled && started && (
        <p className="text-sm text-subtle">{t('staffClasses.started')}</p>
      )}
    </li>
  );
}

const knownProblems = [
  'class-started',
  'class-cancelled',
  'capacity-below-bookings',
  'class-type-exists',
] as const;

/** The problem code if the page has a text for it, else "unexpected". */
export function knownProblem(code: string): string {
  return (knownProblems as readonly string[]).includes(code)
    ? code
    : 'unexpected';
}
