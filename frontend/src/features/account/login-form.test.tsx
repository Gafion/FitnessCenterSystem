import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import '@/i18n';
import { LoginForm } from './login-form';

test('sends the email and password', async () => {
  const onSubmit = vi.fn().mockResolvedValue(true);
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
  render(<LoginForm onSubmit={vi.fn().mockResolvedValue(false)} />);

  fireEvent.click(screen.getByRole('button', { name: 'Log in' }));

  expect(
    await screen.findByText('The email or password is wrong.'),
  ).toBeInTheDocument();
});
