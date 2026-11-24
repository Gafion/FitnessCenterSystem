import { createFileRoute } from '@tanstack/react-router';
import { MailIcon } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { AuthLayout } from '@/features/account/auth-layout';
import { ResendConfirmation } from '@/features/account/resend-confirmation';
import { TextLink } from '@/features/account/text-link';

// After a password signup (D-038): the member confirms their email before logging in.
export const Route = createFileRoute('/signup_/check-email')({
  validateSearch: (search: Record<string, unknown>): { email: string } => ({
    email: typeof search.email === 'string' ? search.email : '',
  }),
  component: CheckEmailPage,
});

function CheckEmailPage() {
  const { t } = useTranslation();
  const { email } = Route.useSearch();

  return (
    <AuthLayout
      title={t('checkEmail.title')}
      footer={<TextLink to="/login">{t('login.title')}</TextLink>}
    >
      <MailIcon aria-hidden="true" className="size-8 text-primary" />
      <p>{t('checkEmail.body', { email })}</p>
      <p className="text-sm text-muted-foreground">{t('checkEmail.noEmail')}</p>
      {email && <ResendConfirmation email={email} />}
    </AuthLayout>
  );
}
