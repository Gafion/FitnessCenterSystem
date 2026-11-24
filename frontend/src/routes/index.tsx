import { createFileRoute, Link } from '@tanstack/react-router';
import { cn } from 'cn';
import { ArrowRightIcon } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Page } from '@/components/shell/page';
import { Button, focusRing } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { formatDay, formatTime } from '@/features/classes/class-time';
import { api, type Schemas } from '@/lib/api';
import { formatOre } from '@/lib/money';

/** How many of the next classes the landing page shows. */
const teaserSize = 3;

export const Route = createFileRoute('/')({
  loader: async () => {
    // The landing page must render even when the API is down, so a failed call
    // leaves its section out instead of failing the page.
    const [terms, schedule] = await Promise.all([
      api
        .GET('/api/members/signup/terms')
        .then(({ data }) => data ?? null)
        .catch(() => null),
      api
        .GET('/api/booking/schedule')
        .then(({ data }) => data ?? null)
        .catch(() => null),
    ]);
    return { terms, upcoming: schedule?.slice(0, teaserSize) ?? null };
  },
  component: LandingRoute,
});

function LandingRoute() {
  const { terms, upcoming } = Route.useLoaderData();
  return <LandingPage terms={terms} upcoming={upcoming} />;
}

type Props = {
  /** The general membership, or null when it couldn't be loaded. */
  terms: Schemas['SignupTerms'] | null;
  /** The next few classes, or null when the schedule couldn't be loaded. */
  upcoming: Schemas['ScheduleEntry'][] | null;
};

// Placeholder content (D-044): the center's name, opening hours, address and
// contact are replaced after the meeting with the center (U10, FIT-26).
export function LandingPage({ terms, upcoming }: Props) {
  const { t } = useTranslation();

  return (
    <Page width="wide" className="gap-16 sm:gap-20">
      <section aria-labelledby="hero" className="flex max-w-2xl flex-col gap-5">
        <p className="text-eyebrow font-medium text-subtle uppercase">
          {t('landing.eyebrow')}
        </p>
        <h1
          id="hero"
          className="text-[2.5rem] leading-[1.08] font-semibold tracking-[-0.03em] sm:text-6xl"
        >
          {t('landing.title')}
        </h1>
        <p className="text-lg text-muted-foreground">{t('landing.tagline')}</p>
        {/* One primary action; seeing the classes is secondary, logging in is a text link. */}
        <div className="flex flex-col gap-3 sm:flex-row">
          <Button size="lg" render={<Link to="/signup" />}>
            {t('signup.title')}
          </Button>
          <Button size="lg" variant="outline" render={<Link to="/classes" />}>
            {t('landing.classes')}
          </Button>
        </div>
        <p className="text-sm text-muted-foreground">
          {t('signup.haveAccount')}{' '}
          <Link
            to="/login"
            className={cn(
              'rounded-sm font-medium text-primary underline-offset-4 hover:underline',
              focusRing,
            )}
          >
            {t('login.title')}
          </Link>
        </p>
      </section>

      <div className="grid gap-10 md:grid-cols-5 md:gap-6">
        <section
          aria-labelledby="upcoming"
          className="flex flex-col gap-3 md:col-span-3"
        >
          <div className="flex items-baseline justify-between gap-4">
            <h2 id="upcoming" className="text-headline font-semibold">
              {t('landing.upcoming.title')}
            </h2>
            <Link
              to="/classes"
              className={cn(
                'inline-flex min-h-11 items-center gap-1 rounded-sm text-sm font-medium text-primary underline-offset-4 hover:underline md:min-h-0',
                focusRing,
              )}
            >
              {t('landing.upcoming.all')}
              <ArrowRightIcon aria-hidden="true" className="size-4" />
            </Link>
          </div>
          {upcoming && upcoming.length > 0 ? (
            <ul className="divide-y divide-border overflow-hidden rounded-lg border border-border bg-card">
              {upcoming.map((entry) => (
                <li
                  key={entry.classId}
                  className="flex items-center gap-4 px-4 py-4 sm:px-5"
                >
                  <span
                    aria-hidden="true"
                    className="w-14 shrink-0 text-lg font-semibold tabular-nums"
                  >
                    {formatTime(entry.startsAt)}
                  </span>
                  <span className="flex min-w-0 flex-1 flex-col">
                    <span className="font-medium">{entry.className}</span>
                    <span className="text-sm text-subtle">
                      {t('landing.upcoming.when', {
                        day: formatDay(entry.startsAt),
                        time: formatTime(entry.startsAt),
                      })}
                    </span>
                  </span>
                </li>
              ))}
            </ul>
          ) : (
            <p className="rounded-lg border border-dashed border-border-strong p-6 text-muted-foreground">
              {t('landing.upcoming.none')}
            </p>
          )}
        </section>

        {terms && (
          <section
            aria-labelledby="membership"
            className="flex flex-col gap-3 md:col-span-2"
          >
            <p
              aria-hidden="true"
              className="text-headline font-semibold tracking-tight"
            >
              {t('landing.membership.title')}
            </p>
            <Card className="flex-1 gap-5 p-6">
              <h2 id="membership" className="text-base font-medium">
                {terms.membershipName}
              </h2>
              <p className="text-headline font-semibold">
                {t('signup.price', { price: formatOre(terms.monthlyPriceOre) })}
              </p>
              <Button className="mt-auto" render={<Link to="/signup" />}>
                {t('landing.membership.join')}
              </Button>
            </Card>
          </section>
        )}
      </div>

      <section aria-labelledby="visit" className="grid gap-4 sm:grid-cols-3">
        <h2 id="visit" className="text-headline font-semibold sm:col-span-3">
          {t('landing.visit.title')}
        </h2>
        {(['hours', 'address', 'contact'] as const).map((item) => (
          <Card key={item} className="gap-1 p-5">
            <h3 className="text-eyebrow font-medium text-subtle uppercase">
              {t(`landing.visit.${item}`)}
            </h3>
            <p className="text-base text-foreground">
              {t(`landing.visit.${item}Placeholder`)}
            </p>
          </Card>
        ))}
      </section>
    </Page>
  );
}
