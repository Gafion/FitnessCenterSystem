import { screen, within } from '@testing-library/react';
import { expect, test } from 'vitest';
import '@/i18n';
import { classEntry } from '@/features/classes/test-entries';
import type { Schemas } from '@/lib/api';
import { renderWithRouter } from '@/test/render-with-router';
import { LandingPage } from './index';

const terms: Schemas['SignupTerms'] = {
  membershipName: 'General membership',
  monthlyPriceOre: 29_900,
  termsVersion: 'terms-v1',
  termsText: 'Synthetic terms text.',
  earlyStartVersion: 'early-v1',
  earlyStartText: 'Synthetic early-start text.',
};

test('landing page shows the translated title', async () => {
  await renderWithRouter(() => <LandingPage terms={terms} upcoming={[]} />);

  expect(
    await screen.findByRole('heading', { name: 'Welcome to FitnessCenter' }),
  ).toBeInTheDocument();
});

test('the hero links to signup, login and the classes', async () => {
  await renderWithRouter(() => <LandingPage terms={null} upcoming={null} />);

  const hero = await screen.findByRole('region', {
    name: 'Welcome to FitnessCenter',
  });
  expect(within(hero).getByRole('link', { name: 'Sign up' })).toHaveAttribute(
    'href',
    '/signup',
  );
  expect(within(hero).getByRole('link', { name: 'Log in' })).toHaveAttribute(
    'href',
    '/login',
  );
  expect(
    within(hero).getByRole('link', { name: 'See classes' }),
  ).toHaveAttribute('href', '/classes');
});

test('shows the next classes in Copenhagen time', async () => {
  await renderWithRouter(() => (
    <LandingPage terms={terms} upcoming={[classEntry()]} />
  ));

  const upcoming = await screen.findByRole('region', { name: 'Next classes' });
  expect(within(upcoming).getByText('Spinning')).toBeInTheDocument();
  expect(
    within(upcoming).getByText('Tuesday 24 November, 18:00'),
  ).toBeInTheDocument();
  expect(
    within(upcoming).getByRole('link', { name: 'See all classes' }),
  ).toHaveAttribute('href', '/classes');
});

test('says so when there are no classes or the schedule failed', async () => {
  await renderWithRouter(() => <LandingPage terms={terms} upcoming={null} />);

  expect(
    await screen.findByText('No classes are scheduled right now.'),
  ).toBeInTheDocument();
});

test('shows the general membership and its monthly price', async () => {
  await renderWithRouter(() => <LandingPage terms={terms} upcoming={[]} />);

  const membership = await screen.findByRole('region', {
    name: 'General membership',
  });
  expect(
    within(membership).getByText('299,00 kr. per month'),
  ).toBeInTheDocument();
  expect(
    within(membership).getByRole('link', { name: 'Become a member' }),
  ).toHaveAttribute('href', '/signup');
});

test('leaves the membership out when the terms failed to load', async () => {
  await renderWithRouter(() => <LandingPage terms={null} upcoming={[]} />);

  await screen.findByRole('heading', { name: 'Welcome to FitnessCenter' });
  expect(screen.queryByText(/per month/)).not.toBeInTheDocument();
});

test('marks opening hours, address and contact as placeholders', async () => {
  await renderWithRouter(() => <LandingPage terms={terms} upcoming={[]} />);

  const visit = await screen.findByRole('region', { name: 'Visit us' });
  for (const text of [
    'PLACEHOLDER: opening hours',
    'PLACEHOLDER: address',
    'PLACEHOLDER: phone and email',
  ]) {
    expect(within(visit).getByText(text)).toBeInTheDocument();
  }
});
