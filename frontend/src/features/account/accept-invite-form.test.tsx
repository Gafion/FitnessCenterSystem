import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import '@/i18n';
import { AcceptInviteForm } from './accept-invite-form';

test('sends the name and password', async () => {
  const onSubmit = vi.fn().mockResolvedValue(null);
  render(<AcceptInviteForm onSubmit={onSubmit} />);

  fireEvent.change(screen.getByLabelText('First name'), {
    target: { value: 'Test' },
  });
  fireEvent.change(screen.getByLabelText('Last name'), {
    target: { value: 'Staff' },
  });
  fireEvent.change(screen.getByLabelText('Password (at least 8 characters)'), {
    target: { value: 'correct-horse' },
  });
  fireEvent.click(screen.getByRole('button', { name: 'Create staff account' }));

  await waitFor(() =>
    expect(onSubmit).toHaveBeenCalledWith({
      firstName: 'Test',
      lastName: 'Staff',
      password: 'correct-horse',
    }),
  );
});

test('shows the errors the API returns', async () => {
  render(
    <AcceptInviteForm
      onSubmit={vi.fn().mockResolvedValue({
        '': ['This invite has expired. Ask an admin to send a new one.'],
        password: ['Passwords must be at least 8 characters.'],
      })}
    />,
  );

  fireEvent.click(screen.getByRole('button', { name: 'Create staff account' }));

  expect(
    await screen.findByText(
      'This invite has expired. Ask an admin to send a new one.',
    ),
  ).toBeInTheDocument();
  expect(
    screen.getByText('Passwords must be at least 8 characters.'),
  ).toBeInTheDocument();
});
