import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Page } from '@/components/shell/page';
import { Card } from '@/components/ui/card';

/**
 * The frame of the login, signup and email pages: a title, then the content on
 * one card (full width on phones), then an optional line under the card.
 */
export function AuthLayout({
  title,
  description,
  footer,
  children,
}: {
  title: ReactNode;
  description?: ReactNode;
  footer?: ReactNode;
  children: ReactNode;
}) {
  return (
    <Page width="narrow" className="gap-6">
      <div className="flex flex-col gap-2">
        <h1 className="text-headline font-semibold">{title}</h1>
        {description && (
          <div className="text-base text-muted-foreground">{description}</div>
        )}
      </div>
      <Card className="gap-5 p-5 text-base sm:p-6">{children}</Card>
      {footer && <div className="text-sm text-muted-foreground">{footer}</div>}
    </Page>
  );
}

/** A labelled rule between the Google button and the email form. */
export function OrDivider() {
  const { t } = useTranslation();
  return (
    <div className="flex items-center gap-3 text-sm text-subtle">
      <span aria-hidden="true" className="h-px flex-1 bg-border" />
      {t('auth.or')}
      <span aria-hidden="true" className="h-px flex-1 bg-border" />
    </div>
  );
}
