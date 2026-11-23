import { useTranslation } from 'react-i18next';
import { formatDate } from '@/features/payments/payment-date';
import type { Schemas } from '@/lib/api';
import { formatOre } from '@/lib/money';

/** The member's membership type, price, status and dates (D-020, D-022). */
export function MembershipSummary({
  membership,
}: {
  membership: Schemas['MyMembership'];
}) {
  const { t } = useTranslation();
  const rows: [string, string][] = [
    [t('membership.type'), membership.membershipType],
    [
      t('membership.price'),
      t('membership.perMonth', {
        price: formatOre(membership.monthlyPriceOre),
      }),
    ],
    [
      t('membership.status'),
      t(`membership.statuses.${membership.status}`, {
        defaultValue: membership.status,
      }),
    ],
    [t('membership.memberSince'), formatDate(membership.memberSince)],
  ];
  if (membership.paidUntil) {
    rows.push([t('membership.paidUntil'), formatDate(membership.paidUntil)]);
  }
  return (
    <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1">
      {rows.map(([term, value]) => (
        <div key={term} className="contents">
          <dt className="text-muted-foreground">{term}</dt>
          <dd>{value}</dd>
        </div>
      ))}
    </dl>
  );
}
