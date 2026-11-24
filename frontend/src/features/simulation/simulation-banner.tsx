import { FlaskConicalIcon } from 'lucide-react';
import { useTranslation } from 'react-i18next';

/**
 * Marks a page that stands in for hardware or a service the MVP doesn't have yet
 * (D-021). Every simulated page shows it, so nobody mistakes it for the real thing.
 */
export function SimulationBanner() {
  const { t } = useTranslation();
  return (
    <aside
      aria-label={t('simulation.label')}
      className="flex gap-3 rounded-lg border-2 border-dashed border-amber-500 bg-amber-50 p-4 text-amber-950 dark:bg-amber-950 dark:text-amber-50"
    >
      <FlaskConicalIcon aria-hidden="true" className="mt-0.5 size-5 shrink-0" />
      <div className="flex flex-col gap-1">
        <p className="text-eyebrow font-semibold uppercase">
          {t('simulation.label')}
        </p>
        <p className="text-sm">{t('simulation.text')}</p>
      </div>
    </aside>
  );
}
