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
] as const;

// The page a staff invite email links to (D-058). The link carries the invite and a
// one-time code; accepting creates the staff account with the role the admin picked.
export const Route = createFileRoute('/accept-invite')({
  validateSearch: (
    search: Record<string, unknown>,
  ): { invite: string; code: string } => ({
    invite: typeof search.invite === 'string' ? search.invite : '',
    code: typeof search.code === 'string' ? search.code : '',
  }),
  component: AcceptInvitePage,
});

function AcceptInvitePage() {
  const { t } = useTranslation();
  const { invite, code } = Route.useSearch();
  const [accepted, setAccepted] = useState(false);

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
    const type = problemType(error);
    return {
      '': [
        (linkProblems as readonly string[]).includes(type)
          ? t(`acceptInvite.${type}`)
          : t('errors.unexpected'),
      ],
    };
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
