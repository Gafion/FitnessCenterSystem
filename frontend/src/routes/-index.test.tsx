import { screen } from '@testing-library/react';
import { expect, test } from 'vitest';
import '@/i18n';
import { renderWithRouter } from '@/test/render-with-router';
import { LandingPage } from './index';

test('landing page shows the translated title', async () => {
  await renderWithRouter(LandingPage);

  expect(
    await screen.findByRole('heading', { name: 'Welcome to FitnessCenter' }),
  ).toBeInTheDocument();
});
