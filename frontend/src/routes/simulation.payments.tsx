import {
  createFileRoute,
  Link,
  notFound,
  redirect,
  useRouter,
} from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
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
    <section className="mx-auto flex max-w-2xl flex-col gap-6 p-8">
      <SimulationBanner />
      <h1 className="text-3xl font-semibold">{t('simulatedPayments.title')}</h1>
      <SimulatedPayments payments={payments} onPay={pay} />
      <PaymentHistory payments={payments} />
      <Link to="/account" className="underline">
        {t('simulatedPayments.back')}
      </Link>
    </section>
  );
}
