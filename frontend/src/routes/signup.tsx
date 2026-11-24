import { createFileRoute, useNavigate } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import { AuthLayout, OrDivider } from '@/features/account/auth-layout';
import { GoogleButton } from '@/features/account/google-button';
import { SignupForm } from '@/features/account/signup-form';
import { TextLink } from '@/features/account/text-link';
import {
  api,
  type FieldErrors,
  getLoginOptions,
  getSignupTerms,
  googleLoginUrl,
  type Schemas,
} from '@/lib/api';

export const Route = createFileRoute('/signup')({
  loader: async () => {
    const [terms, options] = await Promise.all([
      getSignupTerms(),
      getLoginOptions(),
    ]);
    return { terms, options };
  },
  component: SignupPage,
});

function SignupPage() {
  const { t } = useTranslation();
  const { terms, options } = Route.useLoaderData();
  const navigate = useNavigate();

  async function signUp(
    request: Schemas['SignupRequest'],
  ): Promise<FieldErrors | null> {
    const { response, error } = await api.POST('/api/members/signup', {
      body: request,
    });
    if (response.ok) {
      // D-038: a password signup confirms the email before logging in.
      await navigate({
        to: '/signup/check-email',
        search: { email: request.email },
      });
      return null;
    }
    return error?.errors ?? { '': [t('errors.unexpected')] };
  }

  return (
    <AuthLayout
      title={t('signup.title')}
      footer={
        <p>
          {t('signup.haveAccount')}{' '}
          <TextLink to="/login">{t('login.title')}</TextLink>
        </p>
      }
    >
      {options.google && (
        <>
          <GoogleButton href={googleLoginUrl} label="signup.google" />
          <OrDivider />
        </>
      )}
      <SignupForm terms={terms} onSubmit={signUp} />
    </AuthLayout>
  );
}
