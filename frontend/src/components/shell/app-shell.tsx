import { Link } from '@tanstack/react-router';
import {
  Menu,
  MenuContent,
  MenuItem,
  MenuSeparator,
  MenuTrigger,
} from '@ui-sdk/react/menu';
import { cn } from 'cn';
import { MenuIcon } from 'lucide-react';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Button, buttonVariants, focusRing } from '@/components/ui/button';
import { LanguageMenuGroup, LanguageSwitch } from './language-switch';
import { Logo } from './logo';
import { useSession } from './use-session';

const classesLink = { to: '/classes', label: 'shell.nav.classes' } as const;
const accountLink = { to: '/account', label: 'shell.nav.account' } as const;

const menuItem =
  'flex h-11 cursor-pointer items-center rounded-md px-3 text-base text-foreground outline-none data-highlighted:bg-muted';

/**
 * The frame around every page (D-056): a sticky header with the placeholder
 * logo, the main links, the language switch (D-065) and the sign-up action, a
 * menu on phones, and a footer.
 */
export function AppShell({ children }: { children: ReactNode }) {
  const { t } = useTranslation();
  // "My account" only when logged in; "Log in" and "Join" only when logged out.
  // Neither while the session is loading, so they don't flicker (FIT-103).
  const session = useSession();
  const navLinks =
    session === 'logged-in' ? [classesLink, accountLink] : [classesLink];

  return (
    <div className="flex min-h-screen flex-col bg-background text-foreground">
      <a
        href="#main"
        className={cn(
          'sr-only rounded-md bg-card px-3 py-2 focus:not-sr-only focus:absolute focus:top-2 focus:left-2 focus:z-20',
          focusRing,
        )}
      >
        {t('shell.skip')}
      </a>
      <header className="sticky top-0 z-10 border-b border-border bg-background/90 backdrop-blur">
        <div className="mx-auto flex h-14 max-w-5xl items-center gap-6 px-4 sm:px-6">
          <Link
            to="/"
            aria-label={t('shell.home')}
            className={cn('rounded-md', focusRing)}
          >
            <Logo />
          </Link>
          <nav aria-label={t('shell.nav.label')} className="hidden md:block">
            <ul className="flex gap-1">
              {navLinks.map((link) => (
                <li key={link.to}>
                  <Link
                    to={link.to}
                    className={cn(
                      'block rounded-md px-3 py-2 text-sm text-muted-foreground hover:bg-muted hover:text-foreground [&.active]:font-medium [&.active]:text-foreground',
                      focusRing,
                    )}
                  >
                    {t(link.label)}
                  </Link>
                </li>
              ))}
            </ul>
          </nav>
          <div className="ml-auto hidden items-center gap-2 md:flex">
            <LanguageSwitch />
            {session === 'logged-out' && (
              <>
                <Button variant="ghost" render={<Link to="/login" />}>
                  {t('login.title')}
                </Button>
                <Button render={<Link to="/signup" />}>
                  {t('landing.membership.join')}
                </Button>
              </>
            )}
          </div>
          <div className="ml-auto md:hidden">
            <Menu>
              <MenuTrigger
                aria-label={t('shell.menu')}
                className={buttonVariants({ variant: 'outline', size: 'icon' })}
              >
                <MenuIcon aria-hidden="true" className="size-5" />
              </MenuTrigger>
              <MenuContent
                align="end"
                sideOffset={8}
                className="z-30 flex w-64 flex-col gap-1 rounded-lg border border-border bg-popover p-2 text-popover-foreground"
              >
                {navLinks.map((link) => (
                  <MenuItem
                    key={link.to}
                    className={menuItem}
                    render={<Link to={link.to} />}
                  >
                    {t(link.label)}
                  </MenuItem>
                ))}
                {session === 'logged-out' && (
                  <>
                    <MenuItem
                      className={menuItem}
                      render={<Link to="/login" />}
                    >
                      {t('login.title')}
                    </MenuItem>
                    <MenuItem
                      className={cn(
                        buttonVariants(),
                        'mt-1 data-highlighted:bg-primary/90',
                      )}
                      render={<Link to="/signup" />}
                    >
                      {t('landing.membership.join')}
                    </MenuItem>
                  </>
                )}
                <MenuSeparator className="my-1 h-px bg-border" />
                <LanguageMenuGroup />
              </MenuContent>
            </Menu>
          </div>
        </div>
      </header>
      <main id="main" className="flex-1">
        {children}
      </main>
      <footer className="border-t border-border">
        <div className="mx-auto flex max-w-5xl flex-col gap-2 px-4 py-8 text-sm text-subtle sm:flex-row sm:items-center sm:justify-between sm:px-6">
          <Logo />
          <p>{t('shell.footer')}</p>
        </div>
      </footer>
    </div>
  );
}
