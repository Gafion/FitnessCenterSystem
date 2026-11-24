import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { formatDate } from '@/features/payments/payment-date';
import type { Schemas } from '@/lib/api';

export type CancelResult = 'ok' | 'failed';

/**
 * Cancels the membership after one confirmation (D-048). There is no "save" or retention
 * step: the confirmation states the notice rule and nothing else. With a pending
 * cancellation it shows the last day of access instead, and an undo button up to and
 * including that day (D-055). Bookings after the last day are cancelled with the membership,
 * and undoing does not restore them (D-055 amendment, FIT-97).
 */
export function CancelMembership({
  membership,
  onCancel,
  onUndo,
}: {
  membership: Schemas['MyMembership'];
  onCancel: () => Promise<CancelResult>;
  onUndo: () => Promise<CancelResult>;
}) {
  const { t } = useTranslation();
  const [confirming, setConfirming] = useState(false);
  const [busy, setBusy] = useState(false);
  const [failed, setFailed] = useState(false);

  async function undo() {
    setBusy(true);
    setFailed(false);
    const result = await onUndo();
    setBusy(false);
    setFailed(result === 'failed');
  }

  if (membership.cancellationEffectiveOn) {
    return (
      <div className="flex flex-col gap-3">
        <p role="status" className="rounded-md bg-muted px-4 py-3">
          {t('membership.cancellation.pending', {
            date: formatDate(membership.cancellationEffectiveOn),
          })}
        </p>
        {membership.canUndoCancellation && (
          <p className="text-sm text-muted-foreground">
            {t('membership.cancellation.undoKeepsBookingsCancelled')}
          </p>
        )}
        {membership.canUndoCancellation && (
          <Button
            variant="outline"
            className="w-full sm:w-auto sm:self-start"
            disabled={busy}
            onClick={undo}
          >
            {t('membership.cancellation.undo')}
          </Button>
        )}
        {failed && (
          <p role="alert" className="text-sm text-destructive">
            {t('membership.cancellation.undoFailed')}
          </p>
        )}
      </div>
    );
  }

  async function confirm() {
    setBusy(true);
    setFailed(false);
    const result = await onCancel();
    setBusy(false);
    setFailed(result === 'failed');
  }

  if (!confirming) {
    return (
      // Quieter than the page's main actions (FIT-99): a plain text button.
      <Button
        variant="ghost"
        className="-ml-4 self-start text-muted-foreground hover:not-data-disabled:text-destructive"
        onClick={() => setConfirming(true)}
      >
        {t('membership.cancellation.cancel')}
      </Button>
    );
  }

  return (
    <div className="flex flex-col gap-3 rounded-md border border-destructive/30 bg-destructive/5 p-4">
      <p>{t('membership.cancellation.notice')}</p>
      <p className="text-sm text-muted-foreground">
        {t('membership.cancellation.bookings')}
      </p>
      <div className="flex flex-col gap-2 sm:flex-row sm:flex-wrap">
        <Button variant="destructive" disabled={busy} onClick={confirm}>
          {t('membership.cancellation.confirm')}
        </Button>
        <Button
          variant="outline"
          disabled={busy}
          onClick={() => setConfirming(false)}
        >
          {t('membership.cancellation.back')}
        </Button>
      </div>
      {failed && (
        <p role="alert" className="text-sm text-destructive">
          {t('membership.cancellation.failed')}
        </p>
      )}
    </div>
  );
}
