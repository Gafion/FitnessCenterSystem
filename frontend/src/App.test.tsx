import { render, screen } from '@testing-library/react';
import { expect, test } from 'vitest';
import App from './App';

// Smoke test for the placeholder page; replaced by the walking skeleton.
test('renders the app title', () => {
  render(<App />);

  expect(
    screen.getByRole('heading', { name: 'FitnessCenter' }),
  ).toBeInTheDocument();
});
