import { render, screen } from '@testing-library/react';
import { expect, test } from 'vitest';
import '@/i18n';
import { SimulationBanner } from './simulation-banner';

test('marks the page as a simulation', () => {
  render(<SimulationBanner />);

  expect(
    screen.getByRole('complementary', { name: 'Simulation' }),
  ).toHaveTextContent(/synthetic data/);
});
