import { createFileRoute, redirect, useNavigate } from '@tanstack/react-router';
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
    return { account: data, options: await getLoginOptions() };
  },
  component: AccountPage,
});

function AccountPage() {
  const { t } = useTranslation();
  const { account, options } = Route.useLoaderData();
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
      <ErrorMessage code={error} />
      {account.hasGoogleLogin ? (
        <p className="text-sm">{t('account.googleConnected')}</p>
      ) : (
        options.google && (
          <GoogleButton href={googleLinkUrl} label="account.connectGoogle" />
        )
      )}
      <Button variant="outline" onClick={logOut} className="self-start">
        {t('account.logout')}
      </Button>
    </section>
  );
}
