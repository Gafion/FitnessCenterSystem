import {
  createFileRoute,
  Link,
  redirect,
  useNavigate,
} from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { ErrorMessage } from '@/features/account/error-message';
import { GoogleButton } from '@/features/account/google-button';
import { PaymentHistory } from '@/features/payments/payment-history';
import {
  api,
  errorSearch,
  getLoginOptions,
  getMyPayments,
  googleLinkUrl,
  isSimulationEnabled,
} from '@/lib/api';

export const Route = createFileRoute('/account')({
  validateSearch: errorSearch,
  loader: async () => {
    const { data, response } = await api.GET('/api/identity/me');
    if (response.status === 401) {
      throw redirect({ to: '/login', search: {} });
    }
    if (!data) {
      throw new Error('Could not load the account.');
    }
    const [options, card, payments, simulation] = await Promise.all([
      getLoginOptions(),
      api.GET('/api/access/cards/mine'),
      // A failed payments call must not take the rest of the account page with it.
      getMyPayments().catch(() => 'failed' as const),
      // One flag for every simulated page (D-041).
      isSimulationEnabled().catch(() => false),
    ]);
    return {
      account: data,
      options,
      cardUid: card.data?.uid ?? null,
      payments: payments === 'logged-out' ? null : payments,
      simulation,
    };
  },
  component: AccountPage,
});

function AccountPage() {
  const { t } = useTranslation();
  const { account, options, cardUid, payments, simulation } =
    Route.useLoaderData();
  const { error } = Route.useSearch();
  const navigate = useNavigate();

  async function logOut() {
    await api.POST('/api/identity/logout');
    await navigate({ to: '/' });
  }

  return (
    <section className="mx-auto flex max-w-md flex-col gap-6 p-8">
      <h1 className="text-3xl font-semibold">
        {t('account.welcome', { name: account.firstName })}
      </h1>
      <p className="text-muted-foreground">{account.email}</p>
      {cardUid ? (
        <p>
          {t('account.card')} <code className="font-mono">{cardUid}</code>
        </p>
      ) : (
        <p>{t('account.noCard')}</p>
      )}
      <ErrorMessage code={error} />
      {account.hasGoogleLogin ? (
        <p className="text-sm">{t('account.googleConnected')}</p>
      ) : (
        options.google && (
          <GoogleButton href={googleLinkUrl} label="account.connectGoogle" />
        )
      )}
      <Button asChild className="self-start">
        <Link to="/classes">{t('account.classes')}</Link>
      </Button>
      {payments === 'failed' ? (
        <p className="text-sm text-destructive">{t('payments.failed')}</p>
      ) : (
        payments && <PaymentHistory payments={payments} />
      )}
      {simulation && (
        <Button asChild variant="outline" className="self-start">
          <Link to="/simulation/check-in">{t('account.checkIn')}</Link>
        </Button>
      )}
      {payments && payments !== 'failed' && simulation && (
        <Link to="/simulation/payments" className="self-start underline">
          {t('account.simulatedPayments')}
        </Link>
      )}
      <Button variant="outline" onClick={logOut} className="self-start">
        {t('account.logout')}
      </Button>
    </section>
  );
}
