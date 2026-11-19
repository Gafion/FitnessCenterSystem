import { render, screen } from '@testing-library/react';
import { expect, test } from 'vitest';
import '@/i18n';
import { LandingPage } from './index';

test('landing page shows the translated title', () => {
  render(<LandingPage />);

  expect(
    screen.getByRole('heading', { name: 'Welcome to FitnessCenter' }),
  ).toBeInTheDocument();
});
