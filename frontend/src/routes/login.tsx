import { createFileRoute, Link, useNavigate } from '@tanstack/react-router';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ErrorMessage } from '@/features/account/error-message';
import { GoogleButton } from '@/features/account/google-button';
import {
  LoginCodeForm,
  type LoginCodeOutcome,
} from '@/features/account/login-code-form';
import { LoginForm, type LoginOutcome } from '@/features/account/login-form';
import {
  api,
  errorSearch,
  getLoginOptions,
  googleLoginUrl,
  type Schemas,
} from '@/lib/api';

/** `step=code`: the Google login needs the authenticator code next (D-059). */
function loginSearch(search: Record<string, unknown>): {
  error?: string;
  step?: 'code';
} {
  return {
    ...errorSearch(search),
    ...(search.step === 'code' ? { step: 'code' as const } : {}),
  };
}

export const Route = createFileRoute('/login')({
  validateSearch: loginSearch,
  loader: getLoginOptions,
  component: LoginPage,
});

function LoginPage() {
  const { t } = useTranslation();
  const options = Route.useLoaderData();
  const { error, step } = Route.useSearch();
  const navigate = useNavigate();
  const [needsCode, setNeedsCode] = useState(step === 'code');

  async function logIn(
    request: Schemas['LoginRequest'],
  ): Promise<LoginOutcome> {
    const { error: problem, response } = await api.POST('/api/identity/login', {
      body: request,
    });
    if (response.ok) {
      await navigate({ to: '/account' });
      return 'ok';
    }
    if (response.status === 403) {
      // The password was right; the problem's type says what comes next.
      if (problem?.type === 'two-factor-required') {
        setNeedsCode(true);
        return 'ok';
      }
      return 'unconfirmed';
    }
    return 'wrong';
  }

  async function logInWithCode(
    code: string,
    isRecoveryCode: boolean,
  ): Promise<LoginCodeOutcome> {
    const { response } = await api.POST('/api/identity/login/two-factor', {
      body: { code, isRecoveryCode },
    });
    if (response.ok) {
      await navigate({ to: '/account' });
      return 'ok';
    }
    return 'wrong';
  }

  if (needsCode) {
    return (
      <section className="mx-auto flex max-w-md flex-col gap-6 p-8">
        <h1 className="text-3xl font-semibold">{t('login.title')}</h1>
        <LoginCodeForm onSubmit={logInWithCode} />
      </section>
    );
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
