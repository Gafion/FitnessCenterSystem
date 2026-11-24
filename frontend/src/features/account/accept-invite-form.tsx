import { type FormEvent, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import type { FieldErrors } from '@/lib/api';
import { FieldError } from './field-error';

export type AcceptInviteRequest = {
  firstName: string;
  lastName: string;
  password: string;
};

type Props = {
  /** Resolves to null when the invite was accepted, else errors by field ("" for the whole form). */
  onSubmit: (request: AcceptInviteRequest) => Promise<FieldErrors | null>;
};

/** A staff invite (D-058): the invited person gives their name and a password. */
export function AcceptInviteForm({ onSubmit }: Props) {
  const { t } = useTranslation();
  const [errors, setErrors] = useState<FieldErrors>({});
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setSubmitting(true);
    const result = await onSubmit({
      firstName: String(form.get('firstName') ?? ''),
      lastName: String(form.get('lastName') ?? ''),
      password: String(form.get('password') ?? ''),
    });
    setSubmitting(false);
    setErrors(result ?? {});
  }

  return (
    <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-5">
      <FieldError id="invite-error" messages={errors['']} />
      <div className="flex flex-col gap-2">
        <Label htmlFor="firstName">{t('signup.fields.firstName')}</Label>
        <Input
          id="firstName"
          name="firstName"
          autoComplete="given-name"
          required
        />
        <FieldError id="firstName-error" messages={errors.firstName} />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="lastName">{t('signup.fields.lastName')}</Label>
        <Input
          id="lastName"
          name="lastName"
          autoComplete="family-name"
          required
        />
        <FieldError id="lastName-error" messages={errors.lastName} />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="password">{t('signup.fields.password')}</Label>
        <Input
          id="password"
          name="password"
          type="password"
          autoComplete="new-password"
          required
        />
        <FieldError id="password-error" messages={errors.password} />
      </div>
      <Button type="submit" disabled={submitting}>
        {t('acceptInvite.submit')}
      </Button>
    </form>
  );
}
