import QRCode from 'qrcode';
import { type FormEvent, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { FieldError } from './field-error';

export type ConfirmOutcome =
  | { result: 'ok'; recoveryCodes: string[] }
  | { result: 'wrong' };

type Props = {
  /** The setup key in groups of four, for typing it into the app by hand. */
  sharedKey: string;
  /** The standard otpauth://totp/ URI that every TOTP app reads (D-059). */
  authenticatorUri: string;
  /** True for staff: they must finish this before using staff pages (D-013). */
  required: boolean;
  onConfirm: (code: string) => Promise<ConfirmOutcome>;
  onDone: () => void;
};

/**
 * Setting up an authenticator app (D-059): scan the QR code or type the key,
 * confirm one code, then see the recovery codes once.
 */
export function AuthenticatorSetup({
  sharedKey,
  authenticatorUri,
  required,
  onConfirm,
  onDone,
}: Props) {
  const { t } = useTranslation();
  const [qr, setQr] = useState<string>();
  const [wrong, setWrong] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>();

  // Drawn in the browser, so the key never goes to a QR service.
  useEffect(() => {
    let current = true;
    QRCode.toString(authenticatorUri, { type: 'svg', margin: 2 })
      .then((svg) => {
        if (current) {
          setQr(`data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`);
        }
      })
      .catch(() => setQr(undefined));
    return () => {
      current = false;
    };
  }, [authenticatorUri]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setSubmitting(true);
    const outcome = await onConfirm(String(form.get('code') ?? ''));
    setSubmitting(false);
    if (outcome.result === 'ok') {
      setRecoveryCodes(outcome.recoveryCodes);
    } else {
      setWrong(true);
    }
  }

  if (recoveryCodes) {
    return (
      <div className="flex flex-col gap-4">
        <p className="font-medium">{t('authenticator.done')}</p>
        <p>{t('authenticator.recoveryIntro')}</p>
        <ul
          aria-label={t('authenticator.recoveryCodes')}
          className="grid grid-cols-2 gap-2 rounded-md border border-border bg-muted p-4 font-mono text-sm"
        >
          {recoveryCodes.map((code) => (
            <li key={code}>{code}</li>
          ))}
        </ul>
        <Button className="self-start" onClick={onDone}>
          {t('authenticator.continue')}
        </Button>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-5">
      {required && <p>{t('authenticator.required')}</p>}
      <ol className="flex list-decimal flex-col gap-4 pl-5">
        <li>{t('authenticator.stepApp')}</li>
        <li className="flex flex-col gap-3">
          <span>{t('authenticator.stepScan')}</span>
          {qr && (
            <img
              src={qr}
              alt={t('authenticator.qrAlt')}
              width={200}
              height={200}
              className="rounded-md bg-white"
            />
          )}
          <span className="text-sm text-muted-foreground">
            {t('authenticator.keyIntro')}
          </span>
          <code className="self-start rounded-md bg-muted px-3 py-2 font-mono text-sm break-all">
            {sharedKey}
          </code>
        </li>
        <li>{t('authenticator.stepCode')}</li>
      </ol>
      <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-5">
        <FieldError
          id="setup-code-error"
          messages={wrong ? [t('authenticator.wrongCode')] : undefined}
        />
        <div className="flex flex-col gap-2">
          <Label htmlFor="setup-code">{t('authenticator.code')}</Label>
          <Input
            id="setup-code"
            name="code"
            autoComplete="one-time-code"
            inputMode="numeric"
            aria-invalid={wrong || undefined}
            aria-describedby={wrong ? 'setup-code-error' : undefined}
            required
          />
        </div>
        <Button type="submit" disabled={submitting} className="self-start">
          {t('authenticator.confirm')}
        </Button>
      </form>
    </div>
  );
}
