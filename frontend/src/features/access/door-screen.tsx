import { cn } from 'cn';
import { CircleCheckIcon, CircleXIcon } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import type { Schemas } from '@/lib/api';

type SwipeResponse = Schemas['SwipeResponse'];

/** The heading and help line for each refusal. */
const refusals = {
  'unknown-card': ['checkIn.unknownCard', 'checkIn.unknownCardHelp'],
  // D-020, D-042. No name: the screen is public, so it doesn't say who is behind on payments.
  'payment-missing': ['checkIn.paymentMissing', 'checkIn.paymentMissingHelp'],
  // D-048, D-055: access ends when the cancellation takes effect.
  'membership-ended': [
    'checkIn.membershipEnded',
    'checkIn.membershipEndedHelp',
  ],
} as const satisfies Record<
  Exclude<SwipeResponse['outcome'], 'welcome'>,
  readonly [string, string]
>;

/** What the screen at the entrance shows after a swipe (D-003). */
export function DoorScreen({ result }: { result: SwipeResponse }) {
  const { t } = useTranslation();
  const refusal =
    result.outcome === 'welcome' ? null : refusals[result.outcome];
  return (
    <output
      aria-live="polite"
      className={cn(
        'flex min-h-40 flex-col items-center justify-center gap-2 rounded-xl p-6 text-center text-background',
        refusal ? 'bg-destructive' : 'bg-success',
      )}
    >
      {refusal ? (
        <CircleXIcon aria-hidden="true" className="size-10" />
      ) : (
        <CircleCheckIcon aria-hidden="true" className="size-10" />
      )}
      <span className="text-2xl font-semibold">
        {refusal
          ? t(refusal[0])
          : t('checkIn.welcome', { name: result.firstName })}
      </span>
      {refusal && <span className="text-sm">{t(refusal[1])}</span>}
    </output>
  );
}
