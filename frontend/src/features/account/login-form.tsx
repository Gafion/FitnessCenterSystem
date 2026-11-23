import { type FormEvent, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import type { Schemas } from '@/lib/api';
import { FieldError } from './field-error';
import { ResendConfirmation } from './resend-confirmation';

/** "unconfirmed": the password was right, but the email isn't confirmed yet (D-038). */
export type LoginOutcome = 'ok' | 'wrong' | 'unconfirmed';

type Props = {
  onSubmit: (request: Schemas['LoginRequest']) => Promise<LoginOutcome>;
};

export function LoginForm({ onSubmit }: Props) {
  const { t } = useTranslation();
  const [outcome, setOutcome] = useState<LoginOutcome>();
  const [email, setEmail] = useState('');
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);

    const submittedEmail = String(form.get('email') ?? '');

    setSubmitting(true);
    const result = await onSubmit({
      email: submittedEmail,
      password: String(form.get('password') ?? ''),
    });
    setSubmitting(false);
    setEmail(submittedEmail);
    setOutcome(result);
  }

  return (
    <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-5">
      <FieldError
        id="login-error"
        messages={outcome === 'wrong' ? [t('login.failed')] : undefined}
      />
      {outcome === 'unconfirmed' && (
        <div className="flex flex-col gap-3">
          <FieldError id="login-error" messages={[t('login.unconfirmed')]} />
          <ResendConfirmation email={email} />
        </div>
      )}
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
