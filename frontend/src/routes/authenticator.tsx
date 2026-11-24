import { createFileRoute, redirect, useNavigate } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import {
  AuthenticatorSetup,
  type ConfirmOutcome,
} from '@/features/account/authenticator-setup';
import { api } from '@/lib/api';

export const Route = createFileRoute('/authenticator')({
  loader: async () => {
    const [setup, me] = await Promise.all([
      api.GET('/api/identity/authenticator'),
      api.GET('/api/identity/me'),
    ]);
    if (setup.response.status === 401 || me.response.status === 401) {
      throw redirect({ to: '/login', search: {} });
    }
    if (!setup.data || !me.data) {
      throw new Error('Could not load the authenticator setup.');
    }
    return {
      setup: setup.data,
      required: me.data.mustSetUpAuthenticator,
    };
  },
  component: AuthenticatorPage,
});

/** Turn on the authenticator app: mandatory for staff, optional for members (D-013, D-059). */
function AuthenticatorPage() {
  const { t } = useTranslation();
  const { setup, required } = Route.useLoaderData();
  const navigate = useNavigate();

  async function confirm(code: string): Promise<ConfirmOutcome> {
    const { data } = await api.POST('/api/identity/authenticator', {
      body: { code },
    });
    return data
      ? { result: 'ok', recoveryCodes: [...data.codes] }
      : { result: 'wrong' };
  }

  return (
    <section className="mx-auto flex max-w-md flex-col gap-6 p-4 sm:p-8">
      <h1 className="text-3xl font-semibold">{t('authenticator.title')}</h1>
      {setup.enabled || !setup.sharedKey || !setup.authenticatorUri ? (
        <p>{t('authenticator.alreadyOn')}</p>
      ) : (
        <AuthenticatorSetup
          sharedKey={setup.sharedKey}
          authenticatorUri={setup.authenticatorUri}
          required={required}
          onConfirm={confirm}
          onDone={() => navigate({ to: '/account' })}
        />
      )}
    </section>
  );
}
