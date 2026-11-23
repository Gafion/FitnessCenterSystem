import { createFileRoute, redirect, useNavigate } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { api } from '@/lib/api';

export const Route = createFileRoute('/account')({
  loader: async () => {
    const { data, response } = await api.GET('/api/identity/me');
    if (response.status === 401) {
      throw redirect({ to: '/login' });
    }
    if (!data) {
      throw new Error('Could not load the account.');
    }
    return data;
  },
  component: AccountPage,
});

function AccountPage() {
  const { t } = useTranslation();
  const account = Route.useLoaderData();
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
      <Button variant="outline" onClick={logOut} className="self-start">
        {t('account.logout')}
      </Button>
    </section>
  );
}
