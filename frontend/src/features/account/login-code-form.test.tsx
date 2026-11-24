import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import '@/i18n';
import { LoginCodeForm } from './login-code-form';

test('sends the code from the app', async () => {
  const onSubmit = vi.fn().mockResolvedValue('ok');
  render(<LoginCodeForm onSubmit={onSubmit} />);

  fireEvent.change(screen.getByLabelText('Code from the app'), {
    target: { value: '123456' },
  });
  fireEvent.click(screen.getByRole('button', { name: 'Continue' }));

  await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('123456', false));
});

test('a wrong code says so', async () => {
  render(<LoginCodeForm onSubmit={vi.fn().mockResolvedValue('wrong')} />);

  fireEvent.click(screen.getByRole('button', { name: 'Continue' }));

  expect(
    await screen.findByText(
      "That code didn't work. Try the newest code, or log in again.",
    ),
  ).toBeInTheDocument();
});

test('can send a recovery code instead', async () => {
  const onSubmit = vi.fn().mockResolvedValue('ok');
  render(<LoginCodeForm onSubmit={onSubmit} />);

  fireEvent.click(
    screen.getByRole('button', { name: 'Use a recovery code instead' }),
  );
  fireEvent.change(screen.getByLabelText('Recovery code'), {
    target: { value: 'ABCDE-12345' },
  });
  fireEvent.click(screen.getByRole('button', { name: 'Continue' }));

  await waitFor(() =>
    expect(onSubmit).toHaveBeenCalledWith('ABCDE-12345', true),
  );
});
