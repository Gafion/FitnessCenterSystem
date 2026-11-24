import { fireEvent, screen } from '@testing-library/react';
import { expect, test } from 'vitest';
import i18n from '@/i18n';
import { renderWithRouter } from '@/test/render-with-router';
import { AppShell } from './app-shell';

function Shell() {
  return <AppShell>content</AppShell>;
}

test('the header switch changes the language and remembers it', async () => {
  await renderWithRouter(Shell);
  const languages = screen.getByRole('radiogroup', { name: 'Language' });
  expect(screen.getByRole('radio', { name: 'English' })).toHaveAttribute(
    'aria-checked',
    'true',
  );

  fireEvent.click(screen.getByRole('radio', { name: 'Dansk' }));

  expect(await screen.findByRole('link', { name: 'Hold' })).toBeInTheDocument();
  expect(languages).toHaveAccessibleName('Sprog');
  expect(screen.getByRole('radio', { name: 'Dansk' })).toHaveAttribute(
    'aria-checked',
    'true',
  );
  expect(i18n.resolvedLanguage).toBe('da');
  expect(document.documentElement.lang).toBe('da');
  expect(localStorage.getItem('language')).toBe('da');

  fireEvent.click(screen.getByRole('radio', { name: 'English' }));

  expect(
    await screen.findByRole('link', { name: 'Classes' }),
  ).toBeInTheDocument();
  expect(localStorage.getItem('language')).toBe('en');
});
