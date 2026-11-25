import { type FormEvent, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { FieldError } from './field-error';

/** "wrong": a wrong code, or the password step is too old; both look the same. */
export type LoginCodeOutcome = 'ok' | 'wrong';

type Props = {
  onSubmit: (
    code: string,
    isRecoveryCode: boolean,
  ) => Promise<LoginCodeOutcome>;
};

/**
 * The second login step (D-059): the code from the authenticator app, or a
 * recovery code when the phone is lost.
 */
export function LoginCodeForm({ onSubmit }: Props) {
  const { t } = useTranslation();
  const [recovery, setRecovery] = useState(false);
  const [outcome, setOutcome] = useState<LoginCodeOutcome>();
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setSubmitting(true);
    const result = await onSubmit(String(form.get('code') ?? ''), recovery);
    setSubmitting(false);
    setOutcome(result);
  }

  const label = recovery ? t('loginCode.recoveryCode') : t('loginCode.code');
  return (
    <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-5">
      <p>{recovery ? t('loginCode.recoveryIntro') : t('loginCode.intro')}</p>
      <FieldError
        id="code-error"
        messages={outcome === 'wrong' ? [t('loginCode.failed')] : undefined}
      />
      <div className="flex flex-col gap-2">
        <Label htmlFor="code">{label}</Label>
        <Input
          key={recovery ? 'recovery' : 'app'}
          id="code"
          name="code"
          autoComplete="one-time-code"
          inputMode={recovery ? 'text' : 'numeric'}
          aria-invalid={outcome === 'wrong' || undefined}
          aria-describedby={outcome === 'wrong' ? 'code-error' : undefined}
          required
        />
      </div>
      <Button type="submit" disabled={submitting}>
        {t('loginCode.submit')}
      </Button>
      <Button
        type="button"
        variant="link"
        className="self-start"
        onClick={() => {
          setRecovery(!recovery);
          setOutcome(undefined);
        }}
      >
        {recovery ? t('loginCode.useApp') : t('loginCode.useRecovery')}
      </Button>
    </form>
  );
}
