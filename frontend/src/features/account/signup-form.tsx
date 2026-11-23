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

const textFields = ['firstName', 'lastName', 'email', 'password'] as const;

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
      acceptTerms,
      termsVersion: terms.termsVersion,
      startNow,
      earlyStartVersion: terms.earlyStartVersion,
    });
    setSubmitting(false);
    setErrors(result ?? {});
  }

  const fields = google ? (['firstName', 'lastName'] as const) : textFields;
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
      <div>
        <p className="font-medium">{terms.membershipName}</p>
        <p className="text-sm text-muted-foreground">
          {t('signup.price', { price: formatOre(terms.monthlyPriceOre) })}
        </p>
      </div>

      <FieldError id="signup-errors" messages={otherErrors} />

      {google && (
        <p className="text-sm">
          {t('signup.googleEmail', { email: google.email })}
        </p>
      )}

      {fields.map((field) => (
        <div key={field} className="flex flex-col gap-2">
          <Label htmlFor={field}>{t(`signup.fields.${field}`)}</Label>
          <Input
            id={field}
            name={field}
            type={
              field === 'password'
                ? 'password'
                : field === 'email'
                  ? 'email'
                  : 'text'
            }
            autoComplete={autoComplete[field]}
            defaultValue={defaults[field]}
            aria-invalid={errors[field] ? true : undefined}
            aria-describedby={errors[field] ? `${field}-error` : undefined}
            required
          />
          <FieldError id={`${field}-error`} messages={errors[field]} />
        </div>
      ))}

      <section className="flex flex-col gap-3 rounded-md border p-4">
        <p className="whitespace-pre-line text-sm">{terms.termsText}</p>
        <div className="flex items-center gap-2">
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

      <section className="flex flex-col gap-3 rounded-md border p-4">
        <p className="whitespace-pre-line text-sm">{terms.earlyStartText}</p>
        <div className="flex items-center gap-2">
          <Checkbox
            id="startNow"
            checked={startNow}
            onCheckedChange={(checked) => setStartNow(checked === true)}
          />
          <Label htmlFor="startNow">{t('signup.startNow')}</Label>
        </div>
      </section>

      <Button type="submit" disabled={submitting}>
        {t('signup.submit')}
      </Button>
    </form>
  );
}

const autoComplete = {
  firstName: 'given-name',
  lastName: 'family-name',
  email: 'email',
  password: 'new-password',
} as const;
