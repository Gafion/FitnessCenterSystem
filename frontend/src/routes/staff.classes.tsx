import {
  createFileRoute,
  redirect,
  useNavigate,
  useRouter,
} from '@tanstack/react-router';
import type { TFunction } from 'i18next';
import { PlusIcon } from 'lucide-react';
import { type ReactNode, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Page, PageHeader } from '@/components/shell/page';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { TextLink } from '@/features/account/text-link';
import {
  ClassForm,
  type ClassRequest,
  type SaveResult,
} from '@/features/staff/class-form';
import { type ClassTypeResult, ClassTypes } from '@/features/staff/class-types';
import {
  knownProblem,
  StaffClassList,
} from '@/features/staff/staff-class-list';
import { api, type FieldErrors, problemType } from '@/lib/api';

/** How far ahead the list reaches. The API allows at most 93 days. */
const weeksShown = 8;

// Managers and admins plan the center's classes (D-062, D-068). Every endpoint needs
// the Manager policy: other roles get 403 and see a short explanation instead.
export const Route = createFileRoute('/staff/classes')({
  loader: async () => {
    const me = await api.GET('/api/identity/me');
    if (me.response.status === 401) {
      throw redirect({ to: '/login', search: {} });
    }
    // Staff must set up the authenticator app before any staff page (D-013, D-059).
    if (me.data?.mustSetUpAuthenticator) {
      throw redirect({ to: '/authenticator' });
    }
    const now = new Date();
    const to = new Date(now.getTime() + weeksShown * 7 * 86_400_000);
    const [classes, classTypes, instructors] = await Promise.all([
      api.GET('/api/booking/staff/classes', {
        params: { query: { from: now.toISOString(), to: to.toISOString() } },
      }),
      api.GET('/api/booking/staff/class-types'),
      api.GET('/api/identity/staff', {
        params: { query: { role: 'Instructor' } },
      }),
    ]);
    if (classes.response.status === 401) {
      throw redirect({ to: '/login', search: {} });
    }
    if (classes.response.status === 403) {
      return { allowed: false as const };
    }
    if (!classes.data || !classTypes.data || !instructors.data) {
      throw new Error('Could not load the classes.');
    }
    return {
      allowed: true as const,
      now: now.toISOString(),
      classes: classes.data,
      classTypes: classTypes.data,
      instructors: instructors.data,
    };
  },
  component: StaffClassesPage,
});

/** Validation errors by field, or the problem's text under "" (the whole form). */
function errorsFrom(t: TFunction, error: unknown): FieldErrors {
  if (
    typeof error === 'object' &&
    error !== null &&
    'errors' in error &&
    error.errors
  ) {
    return error.errors as FieldErrors;
  }
  return {
    '': [t(`staffClasses.errors.${knownProblem(problemType(error))}`)],
  };
}

function Section({
  id,
  title,
  action,
  children,
}: {
  id: string;
  title: string;
  action?: ReactNode;
  children: ReactNode;
}) {
  return (
    <section aria-labelledby={id} className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 id={id} className="text-title font-semibold">
          {title}
        </h2>
        {action}
      </div>
      {children}
    </section>
  );
}

function StaffClassesPage() {
  const { t } = useTranslation();
  const data = Route.useLoaderData();
  const router = useRouter();
  const navigate = useNavigate();
  const [scheduling, setScheduling] = useState(false);

  if (!data.allowed) {
    return (
      <Page>
        <PageHeader title={t('staffClasses.title')} />
        <div className="flex flex-col gap-4">
          <p>{t('staffClasses.notAllowed')}</p>
          <TextLink to="/account" className="self-start">
            {t('myClasses.backToAccount')}
          </TextLink>
        </div>
      </Page>
    );
  }

  /** Reloads the page after a change, or goes to the login page if the session ended. */
  async function settle(status: number) {
    if (status === 401) {
      await navigate({ to: '/login', search: {} });
      return;
    }
    await router.invalidate();
  }

  async function schedule(request: ClassRequest): Promise<SaveResult> {
    const { error, response } = await api.POST('/api/booking/staff/classes', {
      body: request,
    });
    if (response.ok) {
      await settle(response.status);
      return 'saved';
    }
    return { errors: errorsFrom(t, error) };
  }

  async function change(
    classId: string,
    request: ClassRequest,
  ): Promise<SaveResult> {
    const { error, response } = await api.PUT(
      '/api/booking/staff/classes/{classId}',
      { params: { path: { classId } }, body: request },
    );
    if (response.ok || response.status === 401) {
      await settle(response.status);
      return 'saved';
    }
    return { errors: errorsFrom(t, error) };
  }

  async function cancel(classId: string): Promise<string> {
    const { error, response } = await api.POST(
      '/api/booking/staff/classes/{classId}/cancel',
      { params: { path: { classId } } },
    );
    if (response.ok || response.status === 401) {
      await settle(response.status);
      return 'ok';
    }
    return problemType(error);
  }

  async function createType(name: string): Promise<ClassTypeResult> {
    const { error, response } = await api.POST(
      '/api/booking/staff/class-types',
      { body: { name } },
    );
    if (response.ok) {
      await settle(response.status);
      return 'saved';
    }
    return { errors: errorsFrom(t, error) };
  }

  async function renameType(
    classTypeId: string,
    name: string,
  ): Promise<ClassTypeResult> {
    const { error, response } = await api.PUT(
      '/api/booking/staff/class-types/{classTypeId}',
      { params: { path: { classTypeId } }, body: { name } },
    );
    if (response.ok) {
      await settle(response.status);
      return 'saved';
    }
    return { errors: errorsFrom(t, error) };
  }

  return (
    <Page width="wide">
      <PageHeader
        title={t('staffClasses.title')}
        description={t('staffClasses.intro', { count: weeksShown })}
      />
      <div className="grid gap-10 lg:grid-cols-[minmax(0,2fr)_minmax(0,1fr)]">
        <Section
          id="classes"
          title={t('staffClasses.classesTitle')}
          action={
            !scheduling && (
              <Button onClick={() => setScheduling(true)}>
                <PlusIcon aria-hidden="true" />
                {t('staffClasses.scheduleClass')}
              </Button>
            )
          }
        >
          {scheduling && (
            <Card className="p-5 sm:p-6">
              <ClassForm
                classTypes={data.classTypes}
                instructors={data.instructors}
                onSubmit={schedule}
                onClose={() => setScheduling(false)}
              />
            </Card>
          )}
          <StaffClassList
            classes={data.classes}
            classTypes={data.classTypes}
            instructors={data.instructors}
            now={new Date(data.now)}
            onChange={change}
            onCancel={cancel}
          />
        </Section>
        <Section id="class-types" title={t('staffClasses.types.title')}>
          <ClassTypes
            classTypes={data.classTypes}
            onCreate={createType}
            onRename={renameType}
          />
        </Section>
      </div>
    </Page>
  );
}
