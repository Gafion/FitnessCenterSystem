import { type FormEvent, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import type { Schemas } from '@/lib/api';
import { DoorScreen } from './door-screen';

type SwipeResponse = Schemas['SwipeResponse'];

/**
 * The simulated card reader (D-021): swipe your own card, or type any UID as if
 * someone held that card to the reader. Each swipe goes to the same check-in
 * logic a real reader will use.
 */
export function CardReader({
  cardUid,
  onSwipeMyCard,
  onSwipe,
}: {
  /** The logged-in member's card, or null when they have none. */
  cardUid: string | null;
  /** Resolve to undefined when the reader got no answer. */
  onSwipeMyCard: () => Promise<SwipeResponse | undefined>;
  onSwipe: (cardUid: string) => Promise<SwipeResponse | undefined>;
}) {
  const { t } = useTranslation();
  const [typedUid, setTypedUid] = useState('');
  const [result, setResult] = useState<SwipeResponse | 'failed'>();
  const [busy, setBusy] = useState(false);

  async function swipe(send: () => Promise<SwipeResponse | undefined>) {
    setBusy(true);
    try {
      setResult((await send()) ?? 'failed');
    } finally {
      setBusy(false);
    }
  }

  function swipeTyped(event: FormEvent) {
    event.preventDefault();
    void swipe(() => onSwipe(typedUid));
  }

  return (
    <div className="flex flex-col gap-6">
      <Card className="gap-4 p-5 text-base sm:p-6">
        {cardUid ? (
          <p className="flex flex-wrap items-center gap-2">
            {t('checkIn.yourCard')}{' '}
            <code className="rounded-sm bg-muted px-2 py-1 font-mono text-sm">
              {cardUid}
            </code>
          </p>
        ) : (
          <p className="text-muted-foreground">{t('checkIn.noCard')}</p>
        )}
        <Button
          size="lg"
          className="w-full sm:w-auto sm:self-start"
          disabled={!cardUid || busy}
          onClick={() => void swipe(onSwipeMyCard)}
        >
          {t('checkIn.swipeMyCard')}
        </Button>
      </Card>
      <form
        onSubmit={swipeTyped}
        className="flex flex-col gap-2 rounded-lg border border-border bg-card p-5 sm:p-6"
      >
        <Label htmlFor="card-uid">{t('checkIn.uidLabel')}</Label>
        <div className="flex gap-2">
          <Input
            id="card-uid"
            value={typedUid}
            onChange={(event) => setTypedUid(event.target.value)}
            autoComplete="off"
            spellCheck={false}
            required
          />
          <Button type="submit" variant="outline" disabled={busy}>
            {t('checkIn.swipe')}
          </Button>
        </div>
      </form>
      {result === 'failed' ? (
        <p role="alert" className="font-medium text-destructive">
          {t('checkIn.failed')}
        </p>
      ) : (
        result && <DoorScreen result={result} />
      )}
    </div>
  );
}
