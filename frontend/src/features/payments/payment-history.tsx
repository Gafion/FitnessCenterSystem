import { useTranslation } from 'react-i18next';
import type { Schemas } from '@/lib/api';
import { formatOre } from '@/lib/money';
import { formatDate } from './payment-date';

type Payments = Schemas['MyPayments'];

/** How far the member has paid (D-020). */
export function PaidUntil({ payments }: { payments: Payments }) {
  const { t } = useTranslation();
  return (
    <p>
      {payments.paidUntil
        ? t('payments.paidUntil', { date: formatDate(payments.paidUntil) })
        : t('payments.awaitingFirst')}
    </p>
  );
}

/** The member's payments, newest period first, as the API sends them (D-020). */
export function PaymentHistory({ payments }: { payments: Payments }) {
  const { t } = useTranslation();
  return (
    <section aria-labelledby="payment-history" className="flex flex-col gap-3">
      <h2 id="payment-history" className="text-title font-semibold">
        {t('payments.title')}
      </h2>
      <PaidUntil payments={payments} />
      {payments.payments.length === 0 ? (
        <p className="text-muted-foreground">{t('payments.none')}</p>
      ) : (
        <div className="-mx-1 overflow-x-auto px-1">
          <table className="w-full text-left text-sm">
            <thead className="text-eyebrow font-medium text-subtle uppercase">
              <tr>
                <th scope="col" className="pr-4 pb-2 font-medium">
                  {t('payments.period')}
                </th>
                <th scope="col" className="pr-4 pb-2 text-right font-medium">
                  {t('payments.amount')}
                </th>
                <th scope="col" className="pb-2 font-medium">
                  {t('payments.card')}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border border-t border-border">
              {payments.payments.map((payment) => (
                <tr key={`${payment.periodFrom}-${payment.paidAt}`}>
                  <td className="py-3 pr-4">
                    {t('payments.periodRange', {
                      from: formatDate(payment.periodFrom),
                      to: formatDate(payment.periodTo),
                    })}
                  </td>
                  <td className="py-3 pr-4 text-right font-medium whitespace-nowrap tabular-nums">
                    {formatOre(payment.amountOre)}
                  </td>
                  <td className="py-3 whitespace-nowrap text-muted-foreground">
                    {t('payments.cardEnding', { last4: payment.cardLast4 })}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
