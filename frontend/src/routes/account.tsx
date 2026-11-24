import {
  createFileRoute,
  Link,
  redirect,
  useNavigate,
  useRouter,
} from '@tanstack/react-router';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { ErrorMessage } from '@/features/account/error-message';
import { GoogleButton } from '@/features/account/google-button';
import { MyBookings } from '@/features/classes/my-bookings';
import {
  CancelMembership,
  type CancelResult,
} from '@/features/membership/cancel-membership';
import { MembershipSummary } from '@/features/membership/membership-summary';
import { PaymentHistory } from '@/features/payments/payment-history';
import {
  api,
  cancelMyMembership,
  errorSearch,
  getLoginOptions,
  getMyBookings,
  getMyMembership,
  getMyPayments,
  googleLinkUrl,
  isSimulationEnabled,
  undoMyCancellation,
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
    // Staff must set up the authenticator app before anything else (D-013, D-059).
    if (data.mustSetUpAuthenticator) {
      throw redirect({ to: '/authenticator' });
    }
    // A failed call for one section must not take the rest of the account page with it.
    const [options, card, membership, bookings, payments, simulation] =
      await Promise.all([
        getLoginOptions(),
        api.GET('/api/access/cards/mine'),
        getMyMembership().catch(() => 'failed' as const),
        getMyBookings().catch(() => 'failed' as const),
        getMyPayments().catch(() => 'failed' as const),
        // One flag for every simulated page (D-041).
        isSimulationEnabled().catch(() => false),
      ]);
    return {
      account: data,
      options,
      cardUid: card.data?.uid ?? null,
      membership,
      bookings,
      payments: payments === 'logged-out' ? null : payments,
      simulation,
    };
  },
  component: AccountPage,
});

/** One titled section of the account page. */
function AccountSection({
  id,
  title,
  children,
}: {
  id: string;
  title?: string;
  children: ReactNode;
}) {
  const card = (
    <Card className="h-full text-base">
      {title && (
        <CardHeader>
          <h2 id={id} className="text-xl font-semibold">
            {title}
          </h2>
        </CardHeader>
      )}
      <CardContent className="flex flex-col gap-3">{children}</CardContent>
    </Card>
  );
  // Without a title the content brings its own section and heading.
  return title ? <section aria-labelledby={id}>{card}</section> : card;
}

function AccountPage() {
  const { t } = useTranslation();
  const {
    account,
    options,
    cardUid,
    membership,
    bookings,
    payments,
    simulation,
  } = Route.useLoaderData();
  const { error } = Route.useSearch();
  const navigate = useNavigate();
  const router = useRouter();

  async function cancelMembership(): Promise<CancelResult> {
    const result = await cancelMyMembership().catch(() => 'failed' as const);
    if (result === 'failed') {
      return 'failed';
    }
    if (result === 'logged-out') {
      await navigate({ to: '/login', search: {} });
      return 'ok';
    }
    // Reload after "already-cancelled" too: the page was out of date.
    await router.invalidate();
    return 'ok';
  }

  async function undoCancellation(): Promise<CancelResult> {
    const result = await undoMyCancellation().catch(() => 'failed' as const);
    if (result === 'failed') {
      return 'failed';
    }
    if (result === 'logged-out') {
      await navigate({ to: '/login', search: {} });
      return 'ok';
    }
    // Reload after "ended" too: the page was out of date and hides the button now.
    await router.invalidate();
    return 'ok';
  }

  async function logOut() {
    await api.POST('/api/identity/logout');
    await navigate({ to: '/' });
  }

  return (
    <div className="mx-auto flex max-w-4xl flex-col gap-6 p-4 sm:p-8">
      <h1 className="text-3xl font-semibold">
        {t('account.welcome', { name: account.firstName })}
      </h1>
      <ErrorMessage code={error} />
      <div className="grid gap-6 md:grid-cols-2">
        <AccountSection id="profile" title={t('account.sections.profile')}>
          <p>
            {account.firstName} {account.lastName}
          </p>
          <p className="text-muted-foreground">{account.email}</p>
          {account.hasGoogleLogin ? (
            <p className="text-sm">{t('account.googleConnected')}</p>
          ) : (
            options.google && (
              <GoogleButton
                href={googleLinkUrl}
                label="account.connectGoogle"
              />
            )
          )}
          {account.twoFactorEnabled ? (
            <p className="text-sm">{t('authenticator.on')}</p>
          ) : (
            <Link to="/authenticator" className="self-start text-sm underline">
              {t('authenticator.setUp')}
            </Link>
          )}
          <Button variant="outline" onClick={logOut} className="self-start">
            {t('account.logout')}
          </Button>
        </AccountSection>

        <AccountSection
          id="membership"
          title={t('account.sections.membership')}
        >
          {membership === 'failed' ? (
            <p className="text-sm text-destructive">
              {t('account.membershipFailed')}
            </p>
          ) : membership ? (
            <>
              <MembershipSummary membership={membership} />
              <CancelMembership
                membership={membership}
                onCancel={cancelMembership}
                onUndo={undoCancellation}
              />
            </>
          ) : (
            <p className="text-muted-foreground">{t('account.noMembership')}</p>
          )}
        </AccountSection>

        <AccountSection id="bookings" title={t('account.sections.bookings')}>
          {bookings === 'failed' ? (
            <p className="text-sm text-destructive">
              {t('account.bookingsFailed')}
            </p>
          ) : (
            <MyBookings bookings={bookings} />
          )}
          <Button
            variant="outline"
            className="self-start"
            render={<Link to="/classes" />}
          >
            {t('account.classes')}
          </Button>
        </AccountSection>

        <AccountSection id="card" title={t('account.sections.card')}>
          {cardUid ? (
            <p>
              {t('account.card')} <code className="font-mono">{cardUid}</code>
            </p>
          ) : (
            <p>{t('account.noCard')}</p>
          )}
          {simulation && (
            <Button
              variant="outline"
              className="self-start"
              render={<Link to="/simulation/check-in" />}
            >
              {t('account.checkIn')}
            </Button>
          )}
        </AccountSection>

        {/* PaymentHistory brings its own "Payments" heading. */}
        <AccountSection id="payments">
          {payments === 'failed' ? (
            <p className="text-sm text-destructive">{t('payments.failed')}</p>
          ) : payments ? (
            <PaymentHistory payments={payments} />
          ) : (
            <p className="text-muted-foreground">{t('account.noMembership')}</p>
          )}
          {payments && payments !== 'failed' && simulation && (
            <Link to="/simulation/payments" className="self-start underline">
              {t('account.simulatedPayments')}
            </Link>
          )}
        </AccountSection>
      </div>
    </div>
  );
}
