import { createFileRoute } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';

export const Route = createFileRoute('/')({
  component: LandingPage,
});

// Placeholder landing page (FIT-46); the real content comes with that ticket.
export function LandingPage() {
  const { t } = useTranslation();

  return (
    <section className="mx-auto max-w-3xl p-8">
      <h1 className="text-3xl font-semibold">{t('landing.title')}</h1>
    </section>
  );
}
