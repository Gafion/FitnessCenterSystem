import { createFileRoute, Link } from '@tanstack/react-router';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import {
  AcceptInviteForm,
  type AcceptInviteRequest,
} from '@/features/account/accept-invite-form';
import { api, type FieldErrors, problemType } from '@/lib/api';

const linkProblems = [
  'invite-used',
  'invite-expired',
  'invalid-invite',
  'account-exists',
  'invite-for-another-account',
] as const;

// The page a staff invite email links to (D-058). The link carries the invite and a
// one-time code; accepting creates the staff account with the role the inviter picked.
// When the email already has an account (account=existing), the person logs in and
// accepts, and the role is added to that account (D-064).
export const Route = createFileRoute('/accept-invite')({
  validateSearch: (
    search: Record<string, unknown>,
  ): { invite: string; code: string; account?: 'existing' } => ({
    invite: typeof search.invite === 'string' ? search.invite : '',
    code: typeof search.code === 'string' ? search.code : '',
    ...(search.account === 'existing' ? { account: 'existing' } : {}),
  }),
  component: AcceptInvitePage,
});

function AcceptInvitePage() {
  const { t } = useTranslation();
  const { invite, code, account } = Route.useSearch();
  const [accepted, setAccepted] = useState(false);

  if (account === 'existing' && invite && code) {
    return <AcceptForAccount invite={invite} code={code} />;
  }

  async function accept(
    request: AcceptInviteRequest,
  ): Promise<FieldErrors | null> {
    const { response, error } = await api.POST(
      '/api/identity/staff-invites/accept',
      { body: { inviteId: invite, code, ...request } },
    );
    if (response.ok) {
      setAccepted(true);
      return null;
    }
    if (error && 'errors' in error && error.errors) {
      return error.errors;
    }
    return { '': [problemMessage(t, error)] };
  }

  if (accepted || !invite || !code) {
    return (
      <section className="mx-auto flex max-w-md flex-col gap-6 p-8">
        <h1 className="text-3xl font-semibold">
          {t(accepted ? 'acceptInvite.done' : 'acceptInvite.missing')}
        </h1>
        <p>
          {t(accepted ? 'acceptInvite.doneBody' : 'acceptInvite.missingBody')}
        </p>
        <Button className="self-start" render={<Link to="/login" />}>
          {t('login.title')}
        </Button>
      </section>
    );
  }

  return (
    <section className="mx-auto flex max-w-md flex-col gap-6 p-8">
      <h1 className="text-3xl font-semibold">{t('acceptInvite.title')}</h1>
      <p>{t('acceptInvite.intro')}</p>
      <AcceptInviteForm onSubmit={accept} />
    </section>
  );
}

function problemMessage(t: (key: string) => string, error: unknown): string {
  const type = problemType(error);
  return (linkProblems as readonly string[]).includes(type)
    ? t(`acceptInvite.${type}`)
    : t('errors.unexpected');
}

// An existing account (a member, say) accepts while logged in; no new password (D-064).
function AcceptForAccount({ invite, code }: { invite: string; code: string }) {
  const { t } = useTranslation();
  const [accepted, setAccepted] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function accept() {
    setSubmitting(true);
    const { response, error } = await api.POST(
      '/api/identity/staff-invites/accept-existing',
      { body: { inviteId: invite, code } },
    );
    setSubmitting(false);
    if (response.ok) {
      setAccepted(true);
      setError(null);
    } else if (response.status === 401) {
      setError(t('acceptInvite.notLoggedIn'));
    } else {
      setError(problemMessage(t, error));
    }
  }

  return (
    <section className="mx-auto flex max-w-md flex-col gap-6 p-8">
      <h1 className="text-3xl font-semibold">
        {t(
          accepted ? 'acceptInvite.existingDone' : 'acceptInvite.existingTitle',
        )}
      </h1>
      <p>
        {t(
          accepted
            ? 'acceptInvite.existingDoneBody'
            : 'acceptInvite.existingIntro',
        )}
      </p>
      {error && (
        <p role="alert" className="text-destructive">
          {error}
        </p>
      )}
      {accepted ? (
        <Button className="self-start" render={<Link to="/authenticator" />}>
          {t('authenticator.title')}
        </Button>
      ) : (
        <div className="flex gap-3">
          <Button onClick={accept} disabled={submitting}>
            {t('acceptInvite.existingSubmit')}
          </Button>
          <Button variant="outline" render={<Link to="/login" />}>
            {t('login.title')}
          </Button>
        </div>
      )}
    </section>
  );
}
