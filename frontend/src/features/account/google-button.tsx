import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';

/** A plain link: Google login is a full-page round trip through Google, not a fetch. */
export function GoogleButton({ href, label }: { href: string; label: string }) {
  const { t } = useTranslation();

  return (
    <Button asChild variant="outline">
      <a href={href}>{t(label)}</a>
    </Button>
  );
}
