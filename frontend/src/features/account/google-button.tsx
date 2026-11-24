import { cn } from 'cn';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';

/** A plain link: Google login is a full-page round trip through Google, not a fetch. */
export function GoogleButton({
  href,
  label,
  className,
}: {
  href: string;
  label: string;
  className?: string;
}) {
  const { t } = useTranslation();

  return (
    <Button
      variant="outline"
      className={cn('w-full', className)}
      render={<a href={href} />}
    >
      {t(label)}
    </Button>
  );
}
