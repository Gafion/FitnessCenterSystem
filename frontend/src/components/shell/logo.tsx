import { useTranslation } from 'react-i18next';

/**
 * PLACEHOLDER logo and name (D-044, D-056): a hand-drawn SVG of two fjord
 * water lines in a rounded square, next to the placeholder name
 * "FitnessCenter". Both are replaced once the product has a real name.
 */
export function Logo() {
  const { t } = useTranslation();
  return (
    <span className="inline-flex items-center gap-2 text-base font-semibold tracking-tight">
      <svg
        viewBox="0 0 24 24"
        width="24"
        height="24"
        aria-hidden="true"
        data-placeholder="logo"
        className="text-primary"
      >
        <rect x="1" y="1" width="22" height="22" rx="6" fill="currentColor" />
        <g
          stroke="var(--primary-foreground)"
          strokeWidth="1.8"
          strokeLinecap="round"
          fill="none"
        >
          <path d="M5 9.5c2.3 0 2.3 1.6 4.6 1.6s2.3-1.6 4.6-1.6 2.3 1.6 4.8 1.6" />
          <path
            d="M5 14.5c2.3 0 2.3 1.6 4.6 1.6s2.3-1.6 4.6-1.6 2.3 1.6 4.8 1.6"
            opacity=".6"
          />
        </g>
      </svg>
      {t('app.name')}
    </span>
  );
}
