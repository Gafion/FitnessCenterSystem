import { MailIcon } from 'lucide-react';
import { type FormEvent, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Picker } from '@/components/ui/select';
import { FieldError } from '@/features/account/field-error';
import type { FieldErrors, Schemas } from '@/lib/api';
import { fromCopenhagen, toCopenhagen } from './copenhagen-time';

export type ClassRequest = Schemas['ClassRequest'];

/** What the API answered: saved, or errors by form field ("" for the whole form). */
export type SaveResult = 'saved' | { errors: FieldErrors };

type Props = {
  classTypes: readonly Schemas['StaffClassType'][];
  instructors: readonly Schemas['StaffMember'][];
  /** The class being edited; without it the form schedules a new class. */
  editing?: Schemas['StaffClass'];
  onSubmit: (request: ClassRequest) => Promise<SaveResult>;
  onClose: () => void;
};

// The API's limits (FIT-107 API shapes); it checks them again.
const maxLength = 1440;
const maxCapacity = 1000;

// The API names fields as in the request; the form shows them by its own fields.
const apiFields: Record<string, string> = {
  classTypeId: 'classType',
  instructorAccountId: 'instructor',
  startsAt: 'start',
  lengthMinutes: 'length',
  capacity: 'capacity',
};

/** Maps the API's field names (any case) to the form's. */
function formErrors(errors: FieldErrors): FieldErrors {
  const mapped: FieldErrors = {};
  for (const [key, messages] of Object.entries(errors)) {
    const camel = key.charAt(0).toLowerCase() + key.slice(1);
    const field = apiFields[camel] ?? '';
    mapped[field] = [...(mapped[field] ?? []), ...messages];
  }
  return mapped;
}

// Instructor names sort the Danish way (æ, ø, å last), whatever the UI language.
const danish = new Intl.Collator('da');

function wholeNumber(value: string): number | null {
  return /^\d+$/.test(value.trim()) ? Number(value) : null;
}

/**
 * Schedules a class or edits one (D-062, D-068): class type, instructor, a start on the
 * Copenhagen clock, a length and a capacity. When editing, it says what a change means
 * for the booked members: a new time emails them, and the capacity can't go below the
 * number of bookings.
 */
export function ClassForm({
  classTypes,
  instructors,
  editing,
  onSubmit,
  onClose,
}: Props) {
  const { t } = useTranslation();
  const original = editing ? toCopenhagen(editing.startsAt) : null;
  const [classTypeId, setClassTypeId] = useState(editing?.classTypeId ?? '');
  const [instructorId, setInstructorId] = useState(
    editing?.instructorAccountId ?? '',
  );
  const [date, setDate] = useState(original?.date ?? '');
  const [time, setTime] = useState(original?.time ?? '');
  const [errors, setErrors] = useState<FieldErrors>({});
  const [submitting, setSubmitting] = useState(false);
  const booked = editing?.bookedCount ?? 0;
  const moved =
    original !== null && (date !== original.date || time !== original.time);
  const prefix = editing ? `edit-${editing.classId}` : 'new-class';

  const instructorOptions = [...instructors]
    .sort((a, b) => danish.compare(a.displayName, b.displayName))
    .map((instructor) => ({
      value: instructor.id,
      label: instructor.displayName,
    }));
  const classTypeOptions = classTypes.map((type) => ({
    value: type.id,
    label: type.name,
  }));

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const length = wholeNumber(String(form.get('length') ?? ''));
    const capacity = wholeNumber(String(form.get('capacity') ?? ''));
    const startsAt = fromCopenhagen({ date, time });
    const local: FieldErrors = {};
    if (!classTypeId) {
      local.classType = [t('staffClasses.form.classTypeRequired')];
    }
    if (!instructorId) {
      local.instructor = [t('staffClasses.form.instructorRequired')];
    }
    if (!startsAt) {
      local.start = [t('staffClasses.form.startRequired')];
    }
    if (length === null || length < 1 || length > maxLength) {
      local.length = [t('staffClasses.form.lengthRange', { max: maxLength })];
    }
    if (capacity === null || capacity < 1 || capacity > maxCapacity) {
      local.capacity = [
        t('staffClasses.form.capacityRange', { max: maxCapacity }),
      ];
    } else if (capacity < booked) {
      local.capacity = [
        t('staffClasses.form.capacityBelowBookings', { count: booked }),
      ];
    }
    if (Object.keys(local).length > 0 || !startsAt) {
      setErrors(local);
      return;
    }

    setSubmitting(true);
    const result = await onSubmit({
      classTypeId,
      instructorAccountId: instructorId,
      startsAt,
      lengthMinutes: length as number,
      capacity: capacity as number,
    });
    setSubmitting(false);
    if (result === 'saved') {
      setErrors({});
      onClose();
      return;
    }
    setErrors(formErrors(result.errors));
  }

  function fieldProps(field: string) {
    return {
      'aria-invalid': errors[field] ? true : undefined,
      'aria-describedby': errors[field]
        ? `${prefix}-${field}-error`
        : undefined,
    } as const;
  }

  return (
    <form
      onSubmit={handleSubmit}
      noValidate
      aria-label={
        editing
          ? t('staffClasses.form.editTitle', { className: editing.className })
          : t('staffClasses.form.newTitle')
      }
      className="flex flex-col gap-5"
    >
      <FieldError id={`${prefix}-error`} messages={errors['']} />

      <div className="grid gap-5 sm:grid-cols-2">
        <div className="flex flex-col gap-2">
          <Picker
            id={`${prefix}-class-type`}
            label={t('staffClasses.form.classType')}
            placeholder={t('staffClasses.form.choose')}
            options={classTypeOptions}
            value={classTypeId}
            onValueChange={setClassTypeId}
            invalid={!!errors.classType}
            describedBy={
              errors.classType ? `${prefix}-classType-error` : undefined
            }
          />
          <FieldError
            id={`${prefix}-classType-error`}
            messages={errors.classType}
          />
        </div>
        <div className="flex flex-col gap-2">
          <Picker
            id={`${prefix}-instructor`}
            label={t('staffClasses.form.instructor')}
            placeholder={t('staffClasses.form.choose')}
            options={instructorOptions}
            value={instructorId}
            onValueChange={setInstructorId}
            invalid={!!errors.instructor}
            describedBy={
              errors.instructor ? `${prefix}-instructor-error` : undefined
            }
          />
          <FieldError
            id={`${prefix}-instructor-error`}
            messages={errors.instructor}
          />
        </div>
      </div>

      <fieldset className="flex flex-col gap-2">
        <legend className="mb-2 text-sm font-medium">
          {t('staffClasses.form.start')}
        </legend>
        <div className="grid gap-3 sm:grid-cols-2">
          <div className="flex flex-col gap-2">
            <Label htmlFor={`${prefix}-date`} className="text-muted-foreground">
              {t('staffClasses.form.date')}
            </Label>
            <Input
              id={`${prefix}-date`}
              type="date"
              value={date}
              onChange={(event) => setDate(event.currentTarget.value)}
              {...fieldProps('start')}
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor={`${prefix}-time`} className="text-muted-foreground">
              {t('staffClasses.form.time')}
            </Label>
            <Input
              id={`${prefix}-time`}
              type="time"
              value={time}
              onChange={(event) => setTime(event.currentTarget.value)}
              {...fieldProps('start')}
            />
          </div>
        </div>
        <p className="text-sm text-muted-foreground">
          {t('staffClasses.form.danishTime')}
        </p>
        <FieldError id={`${prefix}-start-error`} messages={errors.start} />
        {moved && booked > 0 && (
          <p
            role="status"
            className="flex gap-2 rounded-md border border-border bg-muted p-3 text-sm"
          >
            <MailIcon
              aria-hidden="true"
              className="mt-0.5 size-4 shrink-0 text-subtle"
            />
            {t('staffClasses.form.moveNotice', { count: booked })}
          </p>
        )}
      </fieldset>

      <div className="grid gap-5 sm:grid-cols-2">
        <div className="flex flex-col gap-2">
          <Label htmlFor={`${prefix}-length`}>
            {t('staffClasses.form.length')}
          </Label>
          <Input
            id={`${prefix}-length`}
            name="length"
            type="number"
            inputMode="numeric"
            min={1}
            max={maxLength}
            defaultValue={editing ? String(editing.lengthMinutes) : undefined}
            {...fieldProps('length')}
          />
          <FieldError id={`${prefix}-length-error`} messages={errors.length} />
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor={`${prefix}-capacity`}>
            {t('staffClasses.form.capacity')}
          </Label>
          <Input
            id={`${prefix}-capacity`}
            name="capacity"
            type="number"
            inputMode="numeric"
            min={Math.max(1, booked)}
            max={maxCapacity}
            defaultValue={editing ? String(editing.capacity) : undefined}
            aria-invalid={errors.capacity ? true : undefined}
            aria-describedby={
              [
                booked > 0 ? `${prefix}-capacity-hint` : '',
                errors.capacity ? `${prefix}-capacity-error` : '',
              ]
                .filter(Boolean)
                .join(' ') || undefined
            }
          />
          {booked > 0 && (
            <p
              id={`${prefix}-capacity-hint`}
              className="text-sm text-muted-foreground"
            >
              {t('staffClasses.form.capacityHint', { count: booked })}
            </p>
          )}
          <FieldError
            id={`${prefix}-capacity-error`}
            messages={errors.capacity}
          />
        </div>
      </div>

      <div className="flex flex-col gap-2 sm:flex-row">
        <Button type="submit" disabled={submitting}>
          {editing
            ? t('staffClasses.form.save')
            : t('staffClasses.form.schedule')}
        </Button>
        <Button
          type="button"
          variant="outline"
          disabled={submitting}
          onClick={onClose}
        >
          {t('staffClasses.form.close')}
        </Button>
      </div>
    </form>
  );
}
