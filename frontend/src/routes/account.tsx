import {
  createFileRoute,
  Link,
  redirect,
  useNavigate,
  useRouter,
} from '@tanstack/react-router';
import { AlertCircleIcon, CheckIcon, LogOutIcon } from 'lucide-react';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Page, PageHeader } from '@/components/shell/page';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { EmailLanguage } from '@/features/account/email-language';
import { ErrorMessage } from '@/features/account/error-message';
import { GoogleButton } from '@/features/account/google-button';
import { TextLink } from '@/features/account/text-link';
import type { BookingResult } from '@/features/classes/class-card';
import { MyBookings } from '@/features/classes/my-bookings';
import { WaitlistOffers } from '@/features/classes/waitlist-offers';
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
  getMyOffers,
  getMyPayments,
  googleLinkUrl,
  isSimulationEnabled,
  problemType,
  setPreferredLanguage,
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
    const [options, card, membership, bookings, offers, payments, simulation] =
      await Promise.all([
        getLoginOptions(),
        api.GET('/api/access/cards/mine'),
        getMyMembership().catch(() => 'failed' as const),
        getMyBookings().catch(() => 'failed' as const),
        getMyOffers().catch(() => []),
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
      offers,
      payments: payments === 'logged-out' ? null : payments,
      simulation,
    };
  },
  component: AccountPage,
});

/** One titled section of the account page: a card with an h2. */
function AccountSection({
  id,
  title,
  className,
  children,
}: {
  id: string;
  title?: string;
  className?: string;
  children: ReactNode;
}) {
  const card = (
    <Card className="h-full gap-4 p-5 text-base sm:p-6">
      {title && (
        <h2 id={id} className="text-title font-semibold">
          {title}
        </h2>
      )}
      <div className="flex flex-1 flex-col gap-4">{children}</div>
    </Card>
  );
  // Without a title the content brings its own section and heading.
  return title ? (
    <section aria-labelledby={id} className={className}>
      {card}
    </section>
  ) : (
    <div className={className}>{card}</div>
  );
}

/** A section's "couldn't load" line. */
function LoadFailed({ children }: { children: ReactNode }) {
  return (
    <p className="flex gap-2 text-sm text-destructive">
      <AlertCircleIcon aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
      {children}
    </p>
  );
}

function AccountPage() {
  const { t } = useTranslation();
  const {
    account,
    options,
    cardUid,
    membership,
    bookings,
    offers,
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

  /** Accepts or declines a spot held from a waitlist (D-067), then reloads the page. */
  async function answerOffer(
    method: 'POST' | 'DELETE',
    classId: string,
  ): Promise<BookingResult> {
    const options = { params: { path: { classId } } };
    const { response, error } =
      method === 'POST'
        ? await api.POST('/api/booking/classes/{classId}/offer', options)
        : await api.DELETE('/api/booking/classes/{classId}/offer', options);
    if (response.status === 401) {
      await navigate({ to: '/login', search: {} });
      return undefined;
    }
    // Reload after a refused answer too: an expired offer is gone from the page.
    await router.invalidate();
    return response.ok ? undefined : problemType(error);
  }

  async function logOut() {
    await api.POST('/api/identity/logout');
    await navigate({ to: '/' });
  }

  return (
    <Page width="wide">
      <PageHeader title={t('account.welcome', { name: account.firstName })} />
      <ErrorMessage code={error} />
      {/* D-067: a spot held from a waitlist. If the offers fail to load, the notice is left
          out rather than shown as an error: the email and the classes page show it too. */}
      <WaitlistOffers
        offers={offers}
        onAccept={(classId) => answerOffer('POST', classId)}
        onDecline={(classId) => answerOffer('DELETE', classId)}
      />
      <div className="grid gap-4 md:grid-cols-2 md:gap-6">
        <AccountSection
          id="membership"
          title={t('account.sections.membership')}
        >
          {membership === 'failed' ? (
            <LoadFailed>{t('account.membershipFailed')}</LoadFailed>
          ) : membership ? (
            <>
              <MembershipSummary membership={membership} />
              {/* Cancelling is a quieter, secondary action, set apart at the bottom. */}
              <div className="mt-auto border-t border-border pt-4">
                <CancelMembership
                  membership={membership}
                  onCancel={cancelMembership}
                  onUndo={undoCancellation}
                />
              </div>
            </>
          ) : (
            <p className="text-muted-foreground">{t('account.noMembership')}</p>
          )}
        </AccountSection>

        <AccountSection id="bookings" title={t('account.sections.bookings')}>
          {bookings === 'failed' ? (
            <LoadFailed>{t('account.bookingsFailed')}</LoadFailed>
          ) : (
            <MyBookings bookings={bookings} />
          )}
          <Button
            className="mt-auto w-full sm:w-auto sm:self-start"
            render={<Link to="/classes" />}
          >
            {t('account.classes')}
          </Button>
        </AccountSection>

        {/* PaymentHistory brings its own "Payments" heading. */}
        <AccountSection id="payments" className="md:col-span-2">
          {payments === 'failed' ? (
            <LoadFailed>{t('payments.failed')}</LoadFailed>
          ) : payments ? (
            <PaymentHistory payments={payments} />
          ) : (
            <p className="text-muted-foreground">{t('account.noMembership')}</p>
          )}
          {payments && payments !== 'failed' && simulation && (
            <TextLink to="/simulation/payments" className="self-start text-sm">
              {t('account.simulatedPayments')}
            </TextLink>
          )}
        </AccountSection>

        <AccountSection id="card" title={t('account.sections.card')}>
          {cardUid ? (
            <p className="flex flex-wrap items-center gap-2">
              {t('account.card')}{' '}
              <code className="rounded-sm bg-muted px-2 py-1 font-mono text-sm">
                {cardUid}
              </code>
            </p>
          ) : (
            <p className="text-muted-foreground">{t('account.noCard')}</p>
          )}
          {simulation && (
            <Button
              variant="outline"
              className="mt-auto w-full sm:w-auto sm:self-start"
              render={<Link to="/simulation/check-in" />}
            >
              {t('account.checkIn')}
            </Button>
          )}
        </AccountSection>

        {/* Admins and managers invite staff (D-058, D-063); the API checks the role too. */}
        {account.staffRoles.some(
          (role) => role === 'Admin' || role === 'Manager',
        ) && (
          <AccountSection id="staff" title={t('account.sections.staff')}>
            <Button
              variant="outline"
              className="w-full sm:w-auto sm:self-start"
              render={<Link to="/staff/invite" />}
            >
              {t('account.inviteStaff')}
            </Button>
          </AccountSection>
        )}

        <AccountSection id="profile" title={t('account.sections.profile')}>
          <div className="flex flex-col gap-0.5">
            <p className="font-medium">
              {account.firstName} {account.lastName}
            </p>
            <p className="text-muted-foreground">{account.email}</p>
          </div>
          {account.hasGoogleLogin ? (
            <p className="flex items-center gap-2 text-sm text-muted-foreground">
              <CheckIcon aria-hidden="true" className="size-4 text-success" />
              {t('account.googleConnected')}
            </p>
          ) : (
            options.google && (
              <GoogleButton
                href={googleLinkUrl}
                label="account.connectGoogle"
                className="sm:w-auto sm:self-start"
              />
            )
          )}
          {account.twoFactorEnabled ? (
            <p className="flex items-center gap-2 text-sm text-muted-foreground">
              <CheckIcon aria-hidden="true" className="size-4 text-success" />
              {t('authenticator.on')}
            </p>
          ) : (
            <TextLink to="/authenticator" className="self-start text-sm">
              {t('authenticator.setUp')}
            </TextLink>
          )}
          <EmailLanguage
            value={account.preferredLanguage}
            onChange={setPreferredLanguage}
          />
          <Button
            variant="ghost"
            onClick={logOut}
            className="mt-auto -ml-4 self-start text-muted-foreground"
          >
            <LogOutIcon aria-hidden="true" />
            {t('account.logout')}
          </Button>
        </AccountSection>
      </div>
    </Page>
  );
}
