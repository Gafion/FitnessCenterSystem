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
      className="rounded-lg border-2 border-dashed border-amber-500 bg-amber-50 p-4 text-amber-950 dark:bg-amber-950 dark:text-amber-50"
    >
      <p className="font-semibold uppercase tracking-wide">
        {t('simulation.label')}
      </p>
      <p className="text-sm">{t('simulation.text')}</p>
    </aside>
  );
}
