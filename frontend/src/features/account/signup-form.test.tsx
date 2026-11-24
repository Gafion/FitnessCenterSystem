import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import '@/i18n';
import type { Schemas } from '@/lib/api';
import { SignupForm } from './signup-form';

const terms: Schemas['SignupTerms'] = {
  membershipName: 'General membership',
  monthlyPriceOre: 29_900,
  termsVersion: 'terms-v1',
  termsText: 'Synthetic terms text.',
  earlyStartVersion: 'early-v1',
  earlyStartText: 'Synthetic early-start text.',
};

function fill(label: RegExp, value: string) {
  fireEvent.change(screen.getByLabelText(label), { target: { value } });
}

test('shows the membership, its price and both consent texts', () => {
  render(<SignupForm terms={terms} onSubmit={vi.fn()} />);

  expect(screen.getByText('General membership')).toBeInTheDocument();
  // Testing Library turns Intl's no-break space into a normal one before matching.
  expect(screen.getByText('299,00 kr. per month')).toBeInTheDocument();
  expect(screen.getByText('Synthetic terms text.')).toBeInTheDocument();
  expect(screen.getByText('Synthetic early-start text.')).toBeInTheDocument();
});

test('sends the fields, the consents and the text versions shown', async () => {
  const onSubmit = vi.fn().mockResolvedValue(null);
  render(<SignupForm terms={terms} onSubmit={onSubmit} />);

  fill(/first name/i, 'Test');
  fill(/last name/i, 'Person');
  fill(/date of birth/i, '1990-01-01');
  fill(/^email/i, 'test.person@example.com');
  fill(/^password/i, 'correct-horse');
  fireEvent.click(screen.getByLabelText('I accept the terms'));
  fireEvent.click(screen.getByRole('button', { name: 'Sign up' }));

  await waitFor(() => expect(onSubmit).toHaveBeenCalledOnce());
  expect(onSubmit).toHaveBeenCalledWith({
    firstName: 'Test',
    lastName: 'Person',
    email: 'test.person@example.com',
    password: 'correct-horse',
    dateOfBirth: '1990-01-01',
    acceptTerms: true,
    termsVersion: 'terms-v1',
    startNow: false,
    earlyStartVersion: 'early-v1',
  });
});

test('shows the errors the API returns next to their fields', async () => {
  const onSubmit = vi.fn().mockResolvedValue({
    password: ['Passwords must be at least 8 characters.'],
    acceptTerms: ['You must accept the terms to sign up.'],
    '': ['Something else went wrong.'],
  });
  render(<SignupForm terms={terms} onSubmit={onSubmit} />);

  fireEvent.click(screen.getByRole('button', { name: 'Sign up' }));

  expect(
    await screen.findByText('Passwords must be at least 8 characters.'),
  ).toBeInTheDocument();
  expect(screen.getByLabelText(/^password/i)).toHaveAccessibleDescription(
    'Passwords must be at least 8 characters.',
  );
  expect(
    screen.getByText('You must accept the terms to sign up.'),
  ).toBeInTheDocument();
  expect(screen.getByText('Something else went wrong.')).toBeInTheDocument();
});

// D-052: members must be 18 or older.
test('says members must be 18 and shows the under-18 error next to the date of birth', async () => {
  const onSubmit = vi.fn().mockResolvedValue({
    dateOfBirth: ['You must be 18 or older to become a member.'],
  });
  render(<SignupForm terms={terms} onSubmit={onSubmit} />);

  const dateOfBirth = screen.getByLabelText(/date of birth/i);
  expect(dateOfBirth).toHaveAttribute('type', 'date');
  expect(dateOfBirth).toHaveAccessibleDescription(
    'You must be 18 or older to become a member.',
  );

  fill(/date of birth/i, '2010-06-15');
  fireEvent.click(screen.getByRole('button', { name: 'Sign up' }));

  await waitFor(() => expect(onSubmit).toHaveBeenCalledOnce());
  expect(onSubmit).toHaveBeenCalledWith(
    expect.objectContaining({ dateOfBirth: '2010-06-15' }),
  );
  expect(dateOfBirth).toHaveAttribute('aria-invalid', 'true');
  expect(
    await screen.findByText('You must be 18 or older to become a member.', {
      selector: '#dateOfBirth-error',
    }),
  ).toBeInTheDocument();
});

test('sends no date of birth when it is left empty', async () => {
  const onSubmit = vi.fn().mockResolvedValue(null);
  render(<SignupForm terms={terms} onSubmit={onSubmit} />);

  fireEvent.click(screen.getByRole('button', { name: 'Sign up' }));

  await waitFor(() => expect(onSubmit).toHaveBeenCalledOnce());
  expect(onSubmit).toHaveBeenCalledWith(
    expect.objectContaining({ dateOfBirth: null }),
  );
});

// D-054: a Google signup asks for the date of birth too.
test('with Google, shows the Google email, prefills the names and asks for the date of birth but no password', async () => {
  const onSubmit = vi.fn().mockResolvedValue(null);
  render(
    <SignupForm
      terms={terms}
      google={{
        email: 'test.googler@example.com',
        firstName: 'Test',
        lastName: null,
      }}
      onSubmit={onSubmit}
    />,
  );

  expect(
    screen.getByText('Your email from Google: test.googler@example.com'),
  ).toBeInTheDocument();
  expect(screen.queryByLabelText(/^password/i)).not.toBeInTheDocument();
  expect(screen.queryByLabelText(/^email/i)).not.toBeInTheDocument();
  expect(screen.getByLabelText(/date of birth/i)).toHaveAccessibleDescription(
    'You must be 18 or older to become a member.',
  );
  expect(screen.getByLabelText(/first name/i)).toHaveValue('Test');

  fill(/last name/i, 'Googler');
  fill(/date of birth/i, '1990-01-01');
  fireEvent.click(screen.getByLabelText('I accept the terms'));
  fireEvent.click(screen.getByRole('button', { name: 'Sign up' }));

  await waitFor(() => expect(onSubmit).toHaveBeenCalledOnce());
  expect(onSubmit).toHaveBeenCalledWith(
    expect.objectContaining({
      firstName: 'Test',
      lastName: 'Googler',
      dateOfBirth: '1990-01-01',
      acceptTerms: true,
    }),
  );
});

test('with Google, shows the under-18 error next to the date of birth', async () => {
  const onSubmit = vi.fn().mockResolvedValue({
    dateOfBirth: ['You must be 18 or older to become a member.'],
  });
  render(
    <SignupForm
      terms={terms}
      google={{
        email: 'test.googler@example.com',
        firstName: 'Test',
        lastName: 'Googler',
      }}
      onSubmit={onSubmit}
    />,
  );

  fill(/date of birth/i, '2010-06-15');
  fireEvent.click(screen.getByLabelText('I accept the terms'));
  fireEvent.click(screen.getByRole('button', { name: 'Sign up' }));

  await waitFor(() => expect(onSubmit).toHaveBeenCalledOnce());
  expect(onSubmit).toHaveBeenCalledWith(
    expect.objectContaining({ dateOfBirth: '2010-06-15' }),
  );
  expect(screen.getByLabelText(/date of birth/i)).toHaveAttribute(
    'aria-invalid',
    'true',
  );
  expect(
    await screen.findByText('You must be 18 or older to become a member.', {
      selector: '#dateOfBirth-error',
    }),
  ).toBeInTheDocument();
});
