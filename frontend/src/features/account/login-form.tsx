import { type FormEvent, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import type { Schemas } from '@/lib/api';
import { FieldError } from './field-error';

type Props = {
  /** Sends the login; resolves to true when it succeeded. */
  onSubmit: (request: Schemas['LoginRequest']) => Promise<boolean>;
};

export function LoginForm({ onSubmit }: Props) {
  const { t } = useTranslation();
  const [failed, setFailed] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);

    setSubmitting(true);
    const succeeded = await onSubmit({
      email: String(form.get('email') ?? ''),
      password: String(form.get('password') ?? ''),
    });
    setSubmitting(false);
    setFailed(!succeeded);
  }

  return (
    <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-5">
      <FieldError
        id="login-error"
        messages={failed ? [t('login.failed')] : undefined}
      />
      <div className="flex flex-col gap-2">
        <Label htmlFor="email">{t('login.email')}</Label>
        <Input
          id="email"
          name="email"
          type="email"
          autoComplete="email"
          required
        />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="password">{t('login.password')}</Label>
        <Input
          id="password"
          name="password"
          type="password"
          autoComplete="current-password"
          required
        />
      </div>
      <Button type="submit" disabled={submitting}>
        {t('login.submit')}
      </Button>
    </form>
  );
}
