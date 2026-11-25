import {
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import i18n from '@/i18n';
import { InviteStaffForm } from './invite-staff-form';

// Synthetic data only.
const email = 'new.instructor@example.com';

function fillEmail(value = email) {
  fireEvent.change(screen.getByLabelText('Email'), { target: { value } });
}

test('an admin is offered every role', () => {
  render(
    <InviteStaffForm
      roles={['Admin', 'Manager', 'FrontDesk', 'Instructor']}
      defaultLanguage="en"
      onSubmit={vi.fn()}
    />,
  );

  for (const name of ['Admin', 'Manager', 'Front desk', 'Instructor']) {
    expect(screen.getByRole('radio', { name })).toBeInTheDocument();
  }
});

test('a manager is offered only front desk and instructor (D-063)', () => {
  render(
    <InviteStaffForm
      roles={['FrontDesk', 'Instructor']}
      defaultLanguage="en"
      onSubmit={vi.fn()}
    />,
  );

  const roleGroup = screen.getByRole('radiogroup', { name: 'Role' });
  expect(within(roleGroup).getAllByRole('radio')).toHaveLength(2);
  expect(screen.queryByRole('radio', { name: 'Admin' })).toBeNull();
  expect(screen.queryByRole('radio', { name: 'Manager' })).toBeNull();
});

test('sends the email, the role and the chosen language', async () => {
  const onSubmit = vi.fn().mockResolvedValue({
    sent: { email, role: 'Instructor', expiresAt: '2026-12-02T12:00:00Z' },
  });
  render(
    <InviteStaffForm
      roles={['FrontDesk', 'Instructor']}
      defaultLanguage="en"
      onSubmit={onSubmit}
    />,
  );

  fillEmail();
  fireEvent.click(screen.getByRole('radio', { name: 'Instructor' }));
  fireEvent.click(screen.getByRole('radio', { name: 'Dansk' }));
  fireEvent.click(screen.getByRole('button', { name: 'Send invite' }));

  await waitFor(() =>
    expect(onSubmit).toHaveBeenCalledWith({
      email,
      role: 'Instructor',
      language: 'da',
    }),
  );
  // 12:00 UTC is 13:00 in Copenhagen in December.
  expect(
    await screen.findByText(
      `Invite sent to ${email} as Instructor. The link works until 2 Dec 2026, 13:00.`,
    ),
  ).toBeInTheDocument();
});

test('asks for an email and a role before sending', async () => {
  const onSubmit = vi.fn();
  render(
    <InviteStaffForm
      roles={['FrontDesk', 'Instructor']}
      defaultLanguage="en"
      onSubmit={onSubmit}
    />,
  );

  fireEvent.click(screen.getByRole('button', { name: 'Send invite' }));

  expect(await screen.findByText('Enter an email address.')).toBeVisible();
  expect(screen.getByText('Choose a role.')).toBeVisible();
  expect(onSubmit).not.toHaveBeenCalled();
});

test('shows the errors the API returns', async () => {
  render(
    <InviteStaffForm
      roles={['Admin', 'Manager', 'FrontDesk', 'Instructor']}
      defaultLanguage="en"
      onSubmit={vi.fn().mockResolvedValue({
        errors: {
          '': ['This account already has this role.'],
          email: ['Enter a valid email address.'],
        },
      })}
    />,
  );

  fillEmail('not-an-email');
  fireEvent.click(screen.getByRole('radio', { name: 'Manager' }));
  fireEvent.click(screen.getByRole('button', { name: 'Send invite' }));

  expect(
    await screen.findByText('This account already has this role.'),
  ).toBeVisible();
  expect(screen.getByText('Enter a valid email address.')).toBeVisible();
});

test('shows the role names in Danish', async () => {
  await i18n.changeLanguage('da');
  render(
    <InviteStaffForm
      roles={['FrontDesk', 'Instructor']}
      defaultLanguage="da"
      onSubmit={vi.fn()}
    />,
  );

  expect(screen.getByRole('radio', { name: 'Reception' })).toBeInTheDocument();
  expect(screen.getByRole('radio', { name: 'Instruktør' })).toBeInTheDocument();
  expect(
    screen.getByRole('button', { name: 'Send invitation' }),
  ).toBeInTheDocument();
});
