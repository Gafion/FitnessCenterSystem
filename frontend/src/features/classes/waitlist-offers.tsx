import { GiftIcon } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import type { Schemas } from '@/lib/api';
import type { BookingResult } from './class-card';
import { formatDateTime, formatTime } from './class-time';

type Props = {
  offers: readonly Schemas['MyOffer'][];
  onAccept: (classId: string) => Promise<BookingResult>;
  onDecline: (classId: string) => Promise<BookingResult>;
};

/**
 * The spots held for the member from a waitlist, with Accept and Decline and the time the
 * offer runs out (D-051, D-067). Shows nothing without an open offer.
 */
export function WaitlistOffers({ offers, onAccept, onDecline }: Props) {
  if (offers.length === 0) {
    return null;
  }
  return (
    <div className="flex flex-col gap-3">
      {offers.map((offer) => (
        <Offer
          key={offer.classId}
          offer={offer}
          onAccept={() => onAccept(offer.classId)}
          onDecline={() => onDecline(offer.classId)}
        />
      ))}
    </div>
  );
}

function Offer({
  offer,
  onAccept,
  onDecline,
}: {
  offer: Schemas['MyOffer'];
  onAccept: () => Promise<BookingResult>;
  onDecline: () => Promise<BookingResult>;
}) {
  const { t } = useTranslation();
  const [error, setError] = useState<string>();
  const [busy, setBusy] = useState(false);
  const titleId = `offer-${offer.classId}`;

  async function run(action: () => Promise<BookingResult>) {
    setBusy(true);
    setError(await action());
    setBusy(false);
  }

  return (
    <section
      aria-labelledby={titleId}
      className="flex flex-col gap-3 rounded-lg border border-success/40 bg-success/5 p-4 sm:flex-row sm:items-center sm:gap-6 sm:p-5"
    >
      <GiftIcon
        aria-hidden="true"
        className="size-5 shrink-0 text-success max-sm:hidden"
      />
      <div className="flex min-w-0 flex-1 flex-col gap-1">
        <h2 id={titleId} className="font-semibold">
          {t('offers.title')}
        </h2>
        <p className="text-sm text-muted-foreground">
          {t('offers.text', {
            className: offer.className,
            start: formatDateTime(offer.startsAt),
            end: formatTime(offer.endsAt),
            expires: formatDateTime(offer.expiresAt),
          })}
        </p>
        {error && (
          <p role="alert" className="text-sm font-medium text-destructive">
            {t(`classes.errors.${error}`, {
              defaultValue: t('errors.unexpected'),
            })}
          </p>
        )}
      </div>
      <div className="flex flex-col gap-2 sm:w-36">
        <Button
          className="w-full"
          disabled={busy}
          onClick={() => run(onAccept)}
        >
          {t('offers.accept')}
        </Button>
        <Button
          variant="outline"
          className="w-full"
          disabled={busy}
          onClick={() => run(onDecline)}
        >
          {t('offers.decline')}
        </Button>
      </div>
    </section>
  );
}
