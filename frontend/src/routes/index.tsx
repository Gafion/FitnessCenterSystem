import { createFileRoute, Link } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';

export const Route = createFileRoute('/')({
  component: LandingPage,
});

// Placeholder landing page (FIT-46); the real content comes with that ticket.
export function LandingPage() {
  const { t } = useTranslation();

  return (
    <section className="mx-auto flex max-w-3xl flex-col gap-6 p-8">
      <h1 className="text-3xl font-semibold">{t('landing.title')}</h1>
      <div className="flex gap-3">
        <Button asChild>
          <Link to="/signup">{t('signup.title')}</Link>
        </Button>
        <Button asChild variant="outline">
          <Link to="/login">{t('login.title')}</Link>
        </Button>
      </div>
    </section>
  );
}
