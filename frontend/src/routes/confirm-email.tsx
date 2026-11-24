import { createFileRoute, Link } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { api } from '@/lib/api';

// The page the confirmation email links to (D-038). The link carries the account and a
// one-time code; confirming here lets the member log in.
export const Route = createFileRoute('/confirm-email')({
  validateSearch: (
    search: Record<string, unknown>,
  ): { account: string; code: string } => ({
    account: typeof search.account === 'string' ? search.account : '',
    code: typeof search.code === 'string' ? search.code : '',
  }),
  loaderDeps: ({ search }) => search,
  loader: async ({ deps }) => {
    if (!deps.account || !deps.code) {
      return { confirmed: false };
    }
    const { response } = await api.POST('/api/identity/confirm-email', {
      body: { accountId: deps.account, code: deps.code },
    });
    return { confirmed: response.ok };
  },
  component: ConfirmEmailPage,
});

function ConfirmEmailPage() {
  const { t } = useTranslation();
  const { confirmed } = Route.useLoaderData();

  return (
    <section className="mx-auto flex max-w-md flex-col gap-6 p-8">
      <h1 className="text-3xl font-semibold">
        {t(confirmed ? 'confirmEmail.done' : 'confirmEmail.failed')}
      </h1>
      <p>
        {t(confirmed ? 'confirmEmail.doneBody' : 'confirmEmail.failedBody')}
      </p>
      <Button className="self-start" render={<Link to="/login" />}>
        {t('login.title')}
      </Button>
    </section>
  );
}
