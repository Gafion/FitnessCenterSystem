import { useTranslation } from 'react-i18next';
import type { Schemas } from '@/lib/api';

/** What the screen at the entrance shows after a swipe (D-003). */
export function DoorScreen({ result }: { result: Schemas['SwipeResponse'] }) {
  const { t } = useTranslation();
  const welcome = result.outcome === 'welcome';
  return (
    <output
      aria-live="polite"
      className={`flex min-h-32 flex-col items-center justify-center rounded-xl p-6 text-center text-white ${
        welcome ? 'bg-green-700' : 'bg-red-700'
      }`}
    >
      <span className="text-2xl font-semibold">
        {welcome
          ? t('checkIn.welcome', { name: result.firstName })
          : t('checkIn.unknownCard')}
      </span>
      {!welcome && (
        <span className="text-sm">{t('checkIn.unknownCardHelp')}</span>
      )}
    </output>
  );
}
