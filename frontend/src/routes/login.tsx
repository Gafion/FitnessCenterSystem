import { createFileRoute, useNavigate } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import { AuthLayout, OrDivider } from '@/features/account/auth-layout';
import { ErrorMessage } from '@/features/account/error-message';
import { GoogleButton } from '@/features/account/google-button';
import { LoginForm, type LoginOutcome } from '@/features/account/login-form';
import { TextLink } from '@/features/account/text-link';
import {
  api,
  errorSearch,
  getLoginOptions,
  googleLoginUrl,
  type Schemas,
} from '@/lib/api';

export const Route = createFileRoute('/login')({
  validateSearch: errorSearch,
  loader: getLoginOptions,
  component: LoginPage,
});

function LoginPage() {
  const { t } = useTranslation();
  const options = Route.useLoaderData();
  const { error } = Route.useSearch();
  const navigate = useNavigate();

  async function logIn(
    request: Schemas['LoginRequest'],
  ): Promise<LoginOutcome> {
    const { response } = await api.POST('/api/identity/login', {
      body: request,
    });
    if (response.ok) {
      await navigate({ to: '/account' });
      return 'ok';
    }
    return response.status === 403 ? 'unconfirmed' : 'wrong';
  }

  return (
    <AuthLayout
      title={t('login.title')}
      footer={
        <p>
          {t('login.noAccount')}{' '}
          <TextLink to="/signup">{t('signup.title')}</TextLink>
        </p>
      }
    >
      <ErrorMessage code={error} />
      {options.google && (
        <>
          <GoogleButton href={googleLoginUrl} label="login.google" />
          <OrDivider />
        </>
      )}
      <LoginForm onSubmit={logIn} />
    </AuthLayout>
  );
}
