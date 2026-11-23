import {
  createFileRoute,
  useNavigate,
  useRouter,
} from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import type { BookingResult } from '@/features/classes/class-card';
import { Schedule } from '@/features/classes/schedule';
import { api } from '@/lib/api';

export const Route = createFileRoute('/classes')({
  loader: async () => {
    // The schedule is open to everyone; "me" only tells whether someone is logged in.
    const [schedule, me] = await Promise.all([
      api.GET('/api/booking/schedule'),
      api.GET('/api/identity/me'),
    ]);
    if (!schedule.data) {
      throw new Error('Could not load the schedule.');
    }
    return { schedule: schedule.data, loggedIn: me.response.ok };
  },
  component: ClassesPage,
});

/** The `type` of an API problem answer, e.g. "class-full", or "unexpected". */
function problemType(error: unknown): string {
  return typeof error === 'object' &&
    error !== null &&
    'type' in error &&
    typeof error.type === 'string'
    ? error.type
    : 'unexpected';
}

function ClassesPage() {
  const { t } = useTranslation();
  const { schedule, loggedIn } = Route.useLoaderData();
  const navigate = useNavigate();
  const router = useRouter();

  async function change(
    method: 'POST' | 'DELETE',
    classId: string,
  ): Promise<BookingResult> {
    const options = { params: { path: { classId } } };
    const { response, error } =
      method === 'POST'
        ? await api.POST('/api/booking/classes/{classId}/booking', options)
        : await api.DELETE('/api/booking/classes/{classId}/booking', options);
    if (response.status === 401) {
      await navigate({ to: '/login', search: {} });
      return undefined;
    }
    // Reload the schedule after a refused attempt too: "class-full" means the page
    // was out of date. The cards stay mounted (keyed by class), so ClassCard keeps
    // the error message it shows.
    await router.invalidate();
    return response.ok ? undefined : problemType(error);
  }

  async function book(classId: string): Promise<BookingResult> {
    if (!loggedIn) {
      await navigate({ to: '/login', search: {} });
      return undefined;
    }
    return change('POST', classId);
  }

  return (
    <section className="mx-auto flex max-w-2xl flex-col gap-6 p-8">
      <h1 className="text-3xl font-semibold">{t('classes.title')}</h1>
      <p className="text-muted-foreground">
        {t(loggedIn ? 'classes.intro' : 'classes.introLoggedOut')}
      </p>
      <Schedule
        entries={schedule}
        now={new Date()}
        onBook={book}
        onCancel={(classId) => change('DELETE', classId)}
      />
    </section>
  );
}
