import { createFileRoute, Link, useNavigate } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import { SignupForm } from '@/features/account/signup-form';
import { api, type FieldErrors, type Schemas } from '@/lib/api';

export const Route = createFileRoute('/signup')({
  loader: async () => {
    const { data, error } = await api.GET('/api/members/signup/terms');
    if (error || !data) {
      throw new Error('Could not load the signup terms.');
    }
    return data;
  },
  component: SignupPage,
});

function SignupPage() {
  const { t } = useTranslation();
  const terms = Route.useLoaderData();
  const navigate = useNavigate();

  async function signUp(
    request: Schemas['SignupRequest'],
  ): Promise<FieldErrors | null> {
    const { response, error } = await api.POST('/api/members/signup', {
      body: request,
    });
    if (response.ok) {
      // The API signs the new member in, so the account page works straight away.
      await navigate({ to: '/account' });
      return null;
    }
    return error?.errors ?? { '': [t('errors.unexpected')] };
  }

  return (
    <section className="mx-auto flex max-w-md flex-col gap-6 p-8">
      <h1 className="text-3xl font-semibold">{t('signup.title')}</h1>
      <SignupForm terms={terms} onSubmit={signUp} />
      <p className="text-sm">
        {t('signup.haveAccount')}{' '}
        <Link to="/login" className="underline">
          {t('login.title')}
        </Link>
      </p>
    </section>
  );
}
