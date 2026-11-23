import { type FormEvent, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import type { Schemas } from '@/lib/api';
import { formatOre } from '@/lib/money';
import { PaidUntil } from './payment-history';

/** An error code from the API (e.g. "period-not-contiguous"), or undefined when it worked. */
export type PaymentResult = string | undefined;

type Props = {
  payments: Schemas['MyPayments'];
  /** Sends a payment; without a start date it pays the next month. */
  onPay: (periodFrom?: string) => Promise<PaymentResult>;
};

/**
 * The payment simulator (D-021, D-041): sends the confirmations the Payments service will
 * send later. Skipping a payment means not sending one; a past period makes one missing.
 */
export function SimulatedPayments({ payments, onPay }: Props) {
  const { t } = useTranslation();
  const [periodFrom, setPeriodFrom] = useState('');
  const [error, setError] = useState<string>();
  const [busy, setBusy] = useState(false);

  async function pay(from?: string) {
    setBusy(true);
    setError(undefined);
    try {
      setError(await onPay(from));
    } finally {
      setBusy(false);
    }
  }

  function payChosen(event: FormEvent) {
    event.preventDefault();
    void pay(periodFrom);
  }

  return (
    <div className="flex flex-col gap-6">
      <PaidUntil payments={payments} />
      <p className="text-muted-foreground">
        {t('simulatedPayments.price', {
          price: formatOre(payments.monthlyPriceOre),
        })}
      </p>
      <Button className="self-start" disabled={busy} onClick={() => void pay()}>
        {t('simulatedPayments.payNext')}
      </Button>
      <form onSubmit={payChosen} className="flex flex-col gap-2">
        <Label htmlFor="period-from">{t('simulatedPayments.periodFrom')}</Label>
        <Input
          id="period-from"
          type="date"
          required
          value={periodFrom}
          onChange={(event) => setPeriodFrom(event.target.value)}
        />
        <p className="text-sm text-muted-foreground">
          {t('simulatedPayments.periodHint')}
        </p>
        <Button
          type="submit"
          variant="outline"
          className="self-start"
          disabled={busy}
        >
          {t('simulatedPayments.payChosen')}
        </Button>
      </form>
      {error && (
        <p role="alert" className="text-sm text-destructive">
          {t(`simulatedPayments.errors.${error}`, {
            defaultValue: t('errors.unexpected'),
          })}
        </p>
      )}
      <p className="text-sm text-muted-foreground">
        {t('simulatedPayments.skip')}
      </p>
    </div>
  );
}
