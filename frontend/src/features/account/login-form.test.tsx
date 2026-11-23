import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import '@/i18n';
import { LoginForm } from './login-form';

test('sends the email and password', async () => {
  const onSubmit = vi.fn().mockResolvedValue('ok');
  render(<LoginForm onSubmit={onSubmit} />);

  fireEvent.change(screen.getByLabelText('Email'), {
    target: { value: 'test.person@example.com' },
  });
  fireEvent.change(screen.getByLabelText('Password'), {
    target: { value: 'correct-horse' },
  });
  fireEvent.click(screen.getByRole('button', { name: 'Log in' }));

  await waitFor(() =>
    expect(onSubmit).toHaveBeenCalledWith({
      email: 'test.person@example.com',
      password: 'correct-horse',
    }),
  );
  expect(
    screen.queryByText('The email or password is wrong.'),
  ).not.toBeInTheDocument();
});

test('says the login failed without saying which part was wrong', async () => {
  render(<LoginForm onSubmit={vi.fn().mockResolvedValue('wrong')} />);

  fireEvent.click(screen.getByRole('button', { name: 'Log in' }));

  expect(
    await screen.findByText('The email or password is wrong.'),
  ).toBeInTheDocument();
});

test('an unconfirmed email is told to confirm and can ask for a new link', async () => {
  render(<LoginForm onSubmit={vi.fn().mockResolvedValue('unconfirmed')} />);

  fireEvent.click(screen.getByRole('button', { name: 'Log in' }));

  expect(
    await screen.findByText(
      'Confirm your email first. We sent you a link when you signed up.',
    ),
  ).toBeInTheDocument();
  expect(
    screen.getByRole('button', { name: 'Send a new link' }),
  ).toBeInTheDocument();
});
