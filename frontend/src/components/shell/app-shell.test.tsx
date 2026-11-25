import { screen, within } from '@testing-library/react';
import { afterEach, expect, test, vi } from 'vitest';
import { api } from '@/lib/api';
import { renderWithRouter } from '@/test/render-with-router';
import { AppShell } from './app-shell';

// The header shows "My account" only when logged in, and "Log in" and "Join"
// only when logged out ("Become a member" in English); neither while the
// session loads (FIT-103). The phone menu is covered by e2e/header.spec.ts.

function Shell() {
  return <AppShell>content</AppShell>;
}

// Synthetic data only.
const account = {
  accountId: '0199a3c0-0000-7000-8000-0000000000aa',
  email: 'test.member@example.com',
  firstName: 'Test',
  lastName: 'Member',
  hasGoogleLogin: false,
  twoFactorEnabled: false,
  mustSetUpAuthenticator: false,
  preferredLanguage: 'en',
};

function answerMe(status: 200 | 401) {
  return vi.spyOn(api, 'GET').mockResolvedValue({
    data: status === 200 ? account : undefined,
    error: status === 200 ? undefined : {},
    response: new Response(null, { status }),
  } as never);
}

afterEach(() => {
  vi.restoreAllMocks();
});

function header() {
  return within(screen.getByRole('banner'));
}

test('logged in: shows My account, hides Log in and Join', async () => {
  answerMe(200);
  await renderWithRouter(Shell);

  expect(
    await header().findByRole('link', { name: 'My account' }),
  ).toBeInTheDocument();
  expect(header().queryByRole('link', { name: 'Log in' })).toBeNull();
  expect(header().queryByRole('link', { name: 'Become a member' })).toBeNull();
});

test('logged out: shows Log in and Join, hides My account', async () => {
  answerMe(401);
  await renderWithRouter(Shell);

  expect(
    await header().findByRole('link', { name: 'Log in' }),
  ).toBeInTheDocument();
  expect(
    header().getByRole('link', { name: 'Become a member' }),
  ).toBeInTheDocument();
  expect(header().queryByRole('link', { name: 'My account' })).toBeNull();
});

test('while the session loads, shows neither set', async () => {
  vi.spyOn(api, 'GET').mockReturnValue(new Promise(() => {}) as never);
  await renderWithRouter(Shell);

  expect(header().getByRole('link', { name: 'Classes' })).toBeInTheDocument();
  expect(header().queryByRole('link', { name: 'My account' })).toBeNull();
  expect(header().queryByRole('link', { name: 'Log in' })).toBeNull();
  expect(header().queryByRole('link', { name: 'Become a member' })).toBeNull();
});
