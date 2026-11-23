import { createFileRoute, Link } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
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
    <div className="mx-auto flex max-w-3xl flex-col gap-10 p-4 sm:p-8">
      <section aria-labelledby="hero" className="flex flex-col gap-4">
        <h1 id="hero" className="text-3xl font-semibold sm:text-4xl">
          {t('landing.title')}
        </h1>
        <p className="text-lg text-muted-foreground">{t('landing.tagline')}</p>
        <div className="flex flex-wrap gap-3">
          <Button asChild>
            <Link to="/signup">{t('signup.title')}</Link>
          </Button>
          <Button asChild variant="outline">
            <Link to="/login">{t('login.title')}</Link>
          </Button>
          <Button asChild variant="outline">
            <Link to="/classes">{t('landing.classes')}</Link>
          </Button>
        </div>
      </section>

      <section aria-labelledby="upcoming" className="flex flex-col gap-3">
        <h2 id="upcoming" className="text-2xl font-semibold">
          {t('landing.upcoming.title')}
        </h2>
        {upcoming && upcoming.length > 0 ? (
          <ul className="flex flex-col gap-2">
            {upcoming.map((entry) => (
              <li
                key={entry.classId}
                className="flex flex-col rounded-md border p-3 sm:flex-row sm:justify-between"
              >
                <span className="font-medium">{entry.className}</span>
                <span className="text-muted-foreground">
                  {t('landing.upcoming.when', {
                    day: formatDay(entry.startsAt),
                    time: formatTime(entry.startsAt),
                  })}
                </span>
              </li>
            ))}
          </ul>
        ) : (
          <p className="text-muted-foreground">{t('landing.upcoming.none')}</p>
        )}
        <Link to="/classes" className="self-start underline">
          {t('landing.upcoming.all')}
        </Link>
      </section>

      {terms && (
        <section aria-labelledby="membership">
          <Card>
            <CardHeader>
              <CardTitle>
                <h2 id="membership" className="text-2xl font-semibold">
                  {terms.membershipName}
                </h2>
              </CardTitle>
              <CardDescription>
                {t('signup.price', { price: formatOre(terms.monthlyPriceOre) })}
              </CardDescription>
            </CardHeader>
            <CardContent>
              <Button asChild>
                <Link to="/signup">{t('landing.membership.join')}</Link>
              </Button>
            </CardContent>
          </Card>
        </section>
      )}

      <section aria-labelledby="visit" className="grid gap-4 sm:grid-cols-3">
        <h2 id="visit" className="text-2xl font-semibold sm:col-span-3">
          {t('landing.visit.title')}
        </h2>
        {(['hours', 'address', 'contact'] as const).map((item) => (
          <div key={item} className="flex flex-col gap-1">
            <h3 className="font-semibold">{t(`landing.visit.${item}`)}</h3>
            <p className="text-muted-foreground">
              {t(`landing.visit.${item}Placeholder`)}
            </p>
          </div>
        ))}
      </section>
    </div>
  );
}
