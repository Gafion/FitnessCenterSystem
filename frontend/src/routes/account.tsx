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
import { api, errorSearch, getLoginOptions, googleLinkUrl } from '@/lib/api';

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
    const [options, card, reader] = await Promise.all([
      getLoginOptions(),
      api.GET('/api/access/cards/mine'),
      api.GET('/api/access/simulation/reader'),
    ]);
    return {
      account: data,
      options,
      cardUid: card.data?.uid ?? null,
      // The reader answers 204 only while the simulation is on (D-041).
      simulation: reader.response.ok,
    };
  },
  component: AccountPage,
});

function AccountPage() {
  const { t } = useTranslation();
  const { account, options, cardUid, simulation } = Route.useLoaderData();
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
      {simulation && (
        <Button asChild variant="outline" className="self-start">
          <Link to="/simulation/check-in">{t('account.checkIn')}</Link>
        </Button>
      )}
      <Button variant="outline" onClick={logOut} className="self-start">
        {t('account.logout')}
      </Button>
    </section>
  );
}
