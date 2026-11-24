import { createFileRoute, notFound, redirect } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import { Page, PageHeader } from '@/components/shell/page';
import { CardReader } from '@/features/access/card-reader';
import { SimulationBanner } from '@/features/simulation/simulation-banner';
import { api } from '@/lib/api';

export const Route = createFileRoute('/simulation/check-in')({
  loader: async () => {
    // Any logged-in account may use the reader, while the simulation is on (D-041).
    // When it's off the API answers 404 and so does this page.
    const reader = await api.GET('/api/access/simulation/reader');
    if (reader.response.status === 401) {
      throw redirect({ to: '/login', search: {} });
    }
    if (reader.response.status === 404) {
      throw notFound();
    }
    if (!reader.response.ok) {
      throw new Error('Could not reach the card reader.');
    }
    const card = await api.GET('/api/access/cards/mine');
    return { cardUid: card.data?.uid ?? null };
  },
  component: CheckInPage,
});

function CheckInPage() {
  const { t } = useTranslation();
  const { cardUid } = Route.useLoaderData();

  return (
    <Page width="narrow" className="gap-6">
      <SimulationBanner />
      <PageHeader title={t('checkIn.title')} description={t('checkIn.intro')} />
      <CardReader
        cardUid={cardUid}
        onSwipeMyCard={async () =>
          (await api.POST('/api/access/simulation/reader/swipes/my-card')).data
        }
        onSwipe={async (uid) =>
          (
            await api.POST('/api/access/simulation/reader/swipes', {
              body: { cardUid: uid },
            })
          ).data
        }
      />
    </Page>
  );
}
