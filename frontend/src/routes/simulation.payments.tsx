import {
  createFileRoute,
  notFound,
  redirect,
  useRouter,
} from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import { Page, PageHeader } from '@/components/shell/page';
import { Card } from '@/components/ui/card';
import { TextLink } from '@/features/account/text-link';
import { PaymentHistory } from '@/features/payments/payment-history';
import {
  type PaymentResult,
  SimulatedPayments,
} from '@/features/payments/simulated-payments';
import { SimulationBanner } from '@/features/simulation/simulation-banner';
import {
  api,
  getMyPayments,
  isSimulationEnabled,
  problemType,
} from '@/lib/api';

export const Route = createFileRoute('/simulation/payments')({
  loader: async () => {
    // Hidden when the simulation is off (D-041); the API answers 404 then too.
    if (!(await isSimulationEnabled())) {
      throw notFound();
    }
    const payments = await getMyPayments();
    if (payments === 'logged-out') {
      throw redirect({ to: '/login', search: {} });
    }
    // The simulator acts on the member's own membership; an account without one has none.
    if (!payments) {
      throw notFound();
    }
    return { payments };
  },
  component: SimulatedPaymentsPage,
});

function SimulatedPaymentsPage() {
  const { t } = useTranslation();
  const { payments } = Route.useLoaderData();
  const router = useRouter();

  async function pay(periodFrom?: string): Promise<PaymentResult> {
    const { response, error } = await api.POST('/api/simulation/payments', {
      body: { periodFrom: periodFrom ?? null },
    });
    await router.invalidate();
    return response.ok ? undefined : problemType(error);
  }

  return (
    <Page className="gap-6">
      <SimulationBanner />
      <TextLink to="/account" className="self-start text-sm">
        {t('simulatedPayments.back')}
      </TextLink>
      <PageHeader title={t('simulatedPayments.title')} />
      <SimulatedPayments payments={payments} onPay={pay} />
      <Card className="p-5 text-base sm:p-6">
        <PaymentHistory payments={payments} />
      </Card>
    </Page>
  );
}
