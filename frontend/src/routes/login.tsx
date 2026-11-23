import { createFileRoute, Link, useNavigate } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import { ErrorMessage } from '@/features/account/error-message';
import { GoogleButton } from '@/features/account/google-button';
import { LoginForm } from '@/features/account/login-form';
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

  async function logIn(request: Schemas['LoginRequest']): Promise<boolean> {
    const { response } = await api.POST('/api/identity/login', {
      body: request,
    });
    if (response.ok) {
      await navigate({ to: '/account' });
    }
    return response.ok;
  }

  return (
    <section className="mx-auto flex max-w-md flex-col gap-6 p-8">
      <h1 className="text-3xl font-semibold">{t('login.title')}</h1>
      <ErrorMessage code={error} />
      {options.google && (
        <GoogleButton href={googleLoginUrl} label="login.google" />
      )}
      <LoginForm onSubmit={logIn} />
      <p className="text-sm">
        {t('login.noAccount')}{' '}
        <Link to="/signup" className="underline">
          {t('signup.title')}
        </Link>
      </p>
    </section>
  );
}
