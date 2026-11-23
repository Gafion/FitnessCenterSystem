import { createFileRoute, Link } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import { ResendConfirmation } from '@/features/account/resend-confirmation';

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
    <section className="mx-auto flex max-w-md flex-col gap-6 p-8">
      <h1 className="text-3xl font-semibold">{t('checkEmail.title')}</h1>
      <p>{t('checkEmail.body', { email })}</p>
      <p className="text-sm text-muted-foreground">{t('checkEmail.noEmail')}</p>
      {email && <ResendConfirmation email={email} />}
      <Link to="/login" className="text-sm underline">
        {t('login.title')}
      </Link>
    </section>
  );
}
