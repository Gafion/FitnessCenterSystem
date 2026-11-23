import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { resendEmailConfirmation } from '@/lib/api';

/** Sends a new confirmation link to `email` (D-038). */
export function ResendConfirmation({ email }: { email: string }) {
  const { t } = useTranslation();
  const [state, setState] = useState<'idle' | 'sending' | 'sent'>('idle');

  async function resend() {
    setState('sending');
    await resendEmailConfirmation(email);
    setState('sent');
  }

  if (state === 'sent') {
    return <p className="text-sm">{t('confirmEmail.resent', { email })}</p>;
  }

  return (
    <Button
      variant="outline"
      onClick={resend}
      disabled={state === 'sending'}
      className="self-start"
    >
      {t('confirmEmail.resend')}
    </Button>
  );
}
