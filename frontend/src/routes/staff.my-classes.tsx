import { createFileRoute, redirect } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import { Page, PageHeader } from '@/components/shell/page';
import { TextLink } from '@/features/account/text-link';
import { MyClasses } from '@/features/staff/my-classes';
import { api } from '@/lib/api';

// An instructor's upcoming classes and who booked them (D-049, D-060). The API answers
// only for the Instructor role (403 otherwise) and only with the instructor's own classes.
export const Route = createFileRoute('/staff/my-classes')({
  loader: async () => {
    const me = await api.GET('/api/identity/me');
    if (me.response.status === 401) {
      throw redirect({ to: '/login', search: {} });
    }
    // Staff must set up the authenticator app before any staff page (D-013, D-059).
    if (me.data?.mustSetUpAuthenticator) {
      throw redirect({ to: '/authenticator' });
    }
    const { data, response } = await api.GET('/api/booking/my-classes');
    if (response.status === 401) {
      throw redirect({ to: '/login', search: {} });
    }
    if (response.status === 403) {
      return { classes: null };
    }
    if (!data) {
      throw new Error('Could not load your classes.');
    }
    return { classes: data };
  },
  component: MyClassesPage,
});

function MyClassesPage() {
  const { t } = useTranslation();
  const { classes } = Route.useLoaderData();

  return (
    <Page>
      <PageHeader
        title={t('myClasses.title')}
        description={classes ? t('myClasses.intro') : undefined}
      />
      {classes ? (
        <MyClasses classes={classes} />
      ) : (
        <div className="flex flex-col gap-4">
          <p>{t('myClasses.notAllowed')}</p>
          <TextLink to="/account" className="self-start">
            {t('myClasses.backToAccount')}
          </TextLink>
        </div>
      )}
    </Page>
  );
}
