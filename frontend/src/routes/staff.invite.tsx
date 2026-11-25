import { createFileRoute, redirect } from '@tanstack/react-router';
import type { TFunction } from 'i18next';
import { useTranslation } from 'react-i18next';
import { Page, PageHeader } from '@/components/shell/page';
import { Card } from '@/components/ui/card';
import { TextLink } from '@/features/account/text-link';
import {
  type InviteResult,
  InviteStaffForm,
  type InviteStaffRequest,
} from '@/features/staff/invite-staff-form';
import { currentLanguage } from '@/i18n';
import { api, problemType } from '@/lib/api';

const inviteProblems = ['role-not-allowed', 'already-has-role'] as const;

// Admins and managers invite people to staff accounts by email (D-058, D-063). The page
// offers only the roles the API says this inviter may give; the API checks it again.
export const Route = createFileRoute('/staff/invite')({
  loader: async () => {
    const me = await api.GET('/api/identity/me');
    if (me.response.status === 401) {
      throw redirect({ to: '/login', search: {} });
    }
    // Staff must set up the authenticator app before any staff page (D-013, D-059).
    if (me.data?.mustSetUpAuthenticator) {
      throw redirect({ to: '/authenticator' });
    }
    const { data, response } = await api.GET(
      '/api/identity/staff-invites/roles',
    );
    if (response.status === 401) {
      throw redirect({ to: '/login', search: {} });
    }
    // Members, front desk and instructors may not invite: 403 (D-063).
    return { roles: data ? [...data.roles] : [] };
  },
  component: InviteStaffPage,
});

function problemMessage(t: TFunction, error: unknown): string {
  const type = problemType(error);
  return (inviteProblems as readonly string[]).includes(type)
    ? t(`staffInvite.errors.${type}`)
    : t('staffInvite.errors.unexpected');
}

function InviteStaffPage() {
  const { t } = useTranslation();
  const { roles } = Route.useLoaderData();

  async function invite(request: InviteStaffRequest): Promise<InviteResult> {
    const { data, error } = await api.POST('/api/identity/staff-invites', {
      body: request,
    });
    if (data) {
      return { sent: data };
    }
    if (error && 'errors' in error && error.errors) {
      return { errors: error.errors };
    }
    return { errors: { '': [problemMessage(t, error)] } };
  }

  return (
    <Page width="narrow">
      <PageHeader
        title={t('staffInvite.title')}
        description={roles.length > 0 ? t('staffInvite.intro') : undefined}
      />
      {roles.length > 0 ? (
        <Card className="p-5 sm:p-6">
          <InviteStaffForm
            roles={roles}
            defaultLanguage={currentLanguage()}
            onSubmit={invite}
          />
        </Card>
      ) : (
        <div className="flex flex-col gap-4">
          <p>{t('staffInvite.notAllowed')}</p>
          <TextLink to="/account" className="self-start">
            {t('staffInvite.backToAccount')}
          </TextLink>
        </div>
      )}
    </Page>
  );
}
