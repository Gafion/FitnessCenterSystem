import { fireEvent, render, screen } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import '@/i18n';
import { EmailLanguage } from './email-language';

test('choosing a language saves it', async () => {
  const onChange = vi.fn().mockResolvedValue(undefined);
  render(<EmailLanguage value="da" onChange={onChange} />);

  expect(screen.getByRole('radio', { name: 'Dansk' })).toBeChecked();
  fireEvent.click(screen.getByRole('radio', { name: 'English' }));

  expect(
    await screen.findByText('Saved. New emails come in this language.'),
  ).toBeInTheDocument();
  expect(onChange).toHaveBeenCalledWith('en');
  expect(screen.getByRole('radio', { name: 'English' })).toBeChecked();
});

test('a failed save says so and keeps the old language', async () => {
  const onChange = vi.fn().mockRejectedValue(new Error('offline'));
  render(<EmailLanguage value="en" onChange={onChange} />);

  fireEvent.click(screen.getByRole('radio', { name: 'Dansk' }));

  expect(
    await screen.findByText(
      'The language could not be saved. Please try again.',
    ),
  ).toBeInTheDocument();
  expect(screen.getByRole('radio', { name: 'English' })).toBeChecked();
});
