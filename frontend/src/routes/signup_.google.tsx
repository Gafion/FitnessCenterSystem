import { createFileRoute, redirect, useNavigate } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import { AuthLayout } from '@/features/account/auth-layout';
import { SignupForm } from '@/features/account/signup-form';
import { api, type FieldErrors, getSignupTerms, type Schemas } from '@/lib/api';

// Where the API sends a new person after Google: they confirm their name, give their date
// of birth and accept the terms before the member is created (D-032, D-037, D-054).
export const Route = createFileRoute('/signup_/google')({
  loader: async () => {
    const { data: google } = await api.GET('/api/identity/external/pending');
    if (!google) {
      throw redirect({ to: '/login', search: { error: 'google-failed' } });
    }
    return { google, terms: await getSignupTerms() };
  },
  component: GoogleSignupPage,
});

function GoogleSignupPage() {
  const { t } = useTranslation();
  const { google, terms } = Route.useLoaderData();
  const navigate = useNavigate();

  async function signUp({
    email: _email,
    password: _password,
    ...request
  }: Schemas['SignupRequest']): Promise<FieldErrors | null> {
    const { response, error } = await api.POST('/api/members/signup/google', {
      body: request,
    });
    if (response.ok) {
      await navigate({ to: '/account' });
      return null;
    }
    return error?.errors ?? { '': [t('errors.unexpected')] };
  }

  return (
    <AuthLayout title={t('signup.googleTitle')}>
      <SignupForm terms={terms} google={google} onSubmit={signUp} />
    </AuthLayout>
  );
}
