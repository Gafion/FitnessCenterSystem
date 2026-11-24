import { type FormEvent, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import type { FieldErrors, Schemas } from '@/lib/api';
import { formatOre } from '@/lib/money';
import { FieldError } from './field-error';

type Props = {
  terms: Schemas['SignupTerms'];
  /** Set when the signup started with Google: the email comes from Google and there's no password. */
  google?: Schemas['PendingExternalLogin'];
  /** Sends the signup; resolves to the validation errors, or null when it succeeded. */
  onSubmit: (request: Schemas['SignupRequest']) => Promise<FieldErrors | null>;
};

const textFields = [
  'firstName',
  'lastName',
  'dateOfBirth',
  'email',
  'password',
] as const;

export function SignupForm({ terms, google, onSubmit }: Props) {
  const { t } = useTranslation();
  const [acceptTerms, setAcceptTerms] = useState(false);
  const [startNow, setStartNow] = useState(false);
  const [errors, setErrors] = useState<FieldErrors>({});
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const value = (name: string) => String(form.get(name) ?? '');

    setSubmitting(true);
    const result = await onSubmit({
      firstName: value('firstName'),
      lastName: value('lastName'),
      email: value('email'),
      password: value('password'),
      // D-052: the API refuses a missing date of birth and anyone under 18.
      dateOfBirth: value('dateOfBirth') || null,
      acceptTerms,
      termsVersion: terms.termsVersion,
      startNow,
      earlyStartVersion: terms.earlyStartVersion,
    });
    setSubmitting(false);
    setErrors(result ?? {});
  }

  // D-054: a Google signup asks for the date of birth too; the email and login come from Google.
  const fields = google
    ? (['firstName', 'lastName', 'dateOfBirth'] as const)
    : textFields;
  const defaults: Partial<Record<(typeof textFields)[number], string>> = google
    ? { firstName: google.firstName ?? '', lastName: google.lastName ?? '' }
    : {};

  // Errors on fields the form doesn't show (or on no field) go at the top.
  const shownFields: string[] = [...fields, 'acceptTerms'];
  const otherErrors = Object.entries(errors)
    .filter(([field]) => !shownFields.includes(field))
    .flatMap(([, messages]) => messages);

  return (
    <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-5">
      <div className="rounded-md bg-muted px-4 py-3">
        <p className="font-medium">{terms.membershipName}</p>
        <p className="text-sm text-muted-foreground">
          {t('signup.price', { price: formatOre(terms.monthlyPriceOre) })}
        </p>
      </div>

      <FieldError id="signup-errors" messages={otherErrors} />

      {google && (
        <p className="text-sm text-muted-foreground">
          {t('signup.googleEmail', { email: google.email })}
        </p>
      )}

      {fields.map((field) => (
        <div key={field} className="flex flex-col gap-2">
          <Label htmlFor={field}>{t(`signup.fields.${field}`)}</Label>
          <Input
            id={field}
            name={field}
            type={inputType[field]}
            autoComplete={autoComplete[field]}
            defaultValue={defaults[field]}
            aria-invalid={errors[field] ? true : undefined}
            aria-describedby={
              [
                field === 'dateOfBirth' ? 'dateOfBirth-hint' : undefined,
                errors[field] ? `${field}-error` : undefined,
              ]
                .filter(Boolean)
                .join(' ') || undefined
            }
            required
          />
          {field === 'dateOfBirth' && (
            <p id="dateOfBirth-hint" className="text-sm text-muted-foreground">
              {t('signup.dateOfBirthHint')}
            </p>
          )}
          <FieldError id={`${field}-error`} messages={errors[field]} />
        </div>
      ))}

      <section className="flex flex-col gap-3 rounded-md border border-border p-4">
        <p className="whitespace-pre-line text-sm text-muted-foreground">
          {terms.termsText}
        </p>
        <div className="flex min-h-11 items-center gap-3">
          <Checkbox
            id="acceptTerms"
            checked={acceptTerms}
            onCheckedChange={(checked) => setAcceptTerms(checked === true)}
            aria-invalid={errors.acceptTerms ? true : undefined}
            aria-describedby={
              errors.acceptTerms ? 'acceptTerms-error' : undefined
            }
          />
          <Label htmlFor="acceptTerms">{t('signup.acceptTerms')}</Label>
        </div>
        <FieldError id="acceptTerms-error" messages={errors.acceptTerms} />
      </section>

      <section className="flex flex-col gap-3 rounded-md border border-border p-4">
        <p className="whitespace-pre-line text-sm text-muted-foreground">
          {terms.earlyStartText}
        </p>
        <div className="flex min-h-11 items-center gap-3">
          <Checkbox
            id="startNow"
            checked={startNow}
            onCheckedChange={(checked) => setStartNow(checked === true)}
          />
          <Label htmlFor="startNow">{t('signup.startNow')}</Label>
        </div>
      </section>

      <Button type="submit" className="w-full" disabled={submitting}>
        {t('signup.submit')}
      </Button>
    </form>
  );
}

const autoComplete = {
  firstName: 'given-name',
  lastName: 'family-name',
  dateOfBirth: 'bday',
  email: 'email',
  password: 'new-password',
} as const;

const inputType = {
  firstName: 'text',
  lastName: 'text',
  dateOfBirth: 'date',
  email: 'email',
  password: 'password',
} as const;
