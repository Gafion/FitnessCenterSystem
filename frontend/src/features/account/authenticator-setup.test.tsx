import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import '@/i18n';
import { AuthenticatorSetup } from './authenticator-setup';

// Synthetic key, not tied to any account.
const key = 'jbsw y3dp ehpk 3pxp';
const uri =
  'otpauth://totp/FitnessCenter:test%40example.com?secret=JBSWY3DPEHPK3PXP&issuer=FitnessCenter&digits=6';

function renderSetup(onConfirm = vi.fn(), onDone = vi.fn(), required = false) {
  render(
    <AuthenticatorSetup
      sharedKey={key}
      authenticatorUri={uri}
      required={required}
      onConfirm={onConfirm}
      onDone={onDone}
    />,
  );
}

test('shows the QR code and the setup key', async () => {
  renderSetup();

  expect(
    await screen.findByAltText('QR code for the authenticator app'),
  ).toHaveAttribute('src', expect.stringMatching(/^data:image\/svg\+xml/));
  expect(screen.getByText(key)).toBeInTheDocument();
});

test('tells staff it is required', () => {
  renderSetup(vi.fn(), vi.fn(), true);

  expect(
    screen.getByText(
      'Staff accounts need an authenticator app before they can use staff pages.',
    ),
  ).toBeInTheDocument();
});

test('a wrong code says so and shows no recovery codes', async () => {
  renderSetup(vi.fn().mockResolvedValue({ result: 'wrong' }));

  fireEvent.change(screen.getByLabelText('Code from the app'), {
    target: { value: '000000' },
  });
  fireEvent.click(screen.getByRole('button', { name: 'Turn on' }));

  expect(
    await screen.findByText(/That code doesn't match/),
  ).toBeInTheDocument();
  expect(screen.queryByRole('list', { name: 'Recovery codes' })).toBeNull();
});

test('a right code shows the recovery codes once, then continues', async () => {
  const onConfirm = vi.fn().mockResolvedValue({
    result: 'ok',
    recoveryCodes: ['AAAAA-11111', 'BBBBB-22222'],
  });
  const onDone = vi.fn();
  renderSetup(onConfirm, onDone);

  fireEvent.change(screen.getByLabelText('Code from the app'), {
    target: { value: '123456' },
  });
  fireEvent.click(screen.getByRole('button', { name: 'Turn on' }));

  await waitFor(() => expect(onConfirm).toHaveBeenCalledWith('123456'));
  const list = await screen.findByRole('list', { name: 'Recovery codes' });
  expect(list).toHaveTextContent('AAAAA-11111');
  expect(list).toHaveTextContent('BBBBB-22222');
  fireEvent.click(screen.getByRole('button', { name: "I've saved them" }));
  expect(onDone).toHaveBeenCalled();
});
