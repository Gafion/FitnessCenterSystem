import { useRouter } from '@tanstack/react-router';
import { cn } from 'cn';
import { AlertCircleIcon, LoaderCircleIcon } from 'lucide-react';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';

const widths = {
  /** Forms and single messages: login, signup, confirm email. */
  narrow: 'max-w-md',
  /** One column of content: classes, simulations. */
  default: 'max-w-2xl',
  /** Several columns on desktop: landing, account. */
  wide: 'max-w-5xl',
} as const;

/** The content area of a page, phone first: 16 px gutters, wider from `sm` up (D-056). */
export function Page({
  width = 'default',
  className,
  children,
}: {
  width?: keyof typeof widths;
  className?: string;
  children: ReactNode;
}) {
  return (
    <div
      className={cn(
        'mx-auto flex w-full flex-col gap-8 px-4 py-8 sm:px-6 sm:py-12',
        widths[width],
        className,
      )}
    >
      {children}
    </div>
  );
}

/** A page's title block: an optional eyebrow, the h1, a lead line and actions. */
export function PageHeader({
  eyebrow,
  title,
  description,
  actions,
}: {
  eyebrow?: ReactNode;
  title: ReactNode;
  description?: ReactNode;
  actions?: ReactNode;
}) {
  return (
    <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div className="flex flex-col gap-2">
        {eyebrow && (
          <div className="text-eyebrow font-medium text-subtle uppercase">
            {eyebrow}
          </div>
        )}
        <h1 className="text-headline font-semibold sm:text-display">{title}</h1>
        {description && (
          <div className="text-base text-muted-foreground">{description}</div>
        )}
      </div>
      {actions && <div className="flex flex-wrap gap-3">{actions}</div>}
    </header>
  );
}

/** Shown while a page's data loads (the router's pending component). */
export function PageLoading() {
  const { t } = useTranslation();
  return (
    <Page>
      <div
        role="status"
        className="flex items-center gap-3 text-muted-foreground"
      >
        <LoaderCircleIcon
          aria-hidden="true"
          className="size-5 animate-spin motion-reduce:animate-none"
        />
        {t('states.loading')}
      </div>
      <div aria-hidden="true" className="flex flex-col gap-3">
        <div className="h-9 w-2/3 animate-pulse rounded-md bg-muted motion-reduce:animate-none" />
        <div className="h-24 animate-pulse rounded-lg bg-muted motion-reduce:animate-none" />
        <div className="h-24 animate-pulse rounded-lg bg-muted motion-reduce:animate-none" />
      </div>
    </Page>
  );
}

/** Shown when a page's data couldn't be loaded (the router's error component). */
export function PageError() {
  const { t } = useTranslation();
  const router = useRouter();
  return (
    <Page width="narrow">
      <div
        role="alert"
        className="flex flex-col items-start gap-4 rounded-lg border border-border bg-card p-6"
      >
        <AlertCircleIcon
          aria-hidden="true"
          className="size-6 text-destructive"
        />
        <h1 className="text-title font-semibold">{t('states.errorTitle')}</h1>
        <p className="text-muted-foreground">{t('states.errorBody')}</p>
        <Button variant="outline" onClick={() => void router.invalidate()}>
          {t('states.retry')}
        </Button>
      </div>
    </Page>
  );
}
