import { Radio, RadioGroup, RadioIndicator } from '@ui-sdk/react/radio';
import {
  SegmentedControl,
  SegmentedControlItem,
} from '@ui-sdk/react/segmented-control';
import { cn } from 'cn';
import { CheckCircle2Icon } from 'lucide-react';
import { type FormEvent, useId, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button, focusRing } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { FieldError } from '@/features/account/field-error';
import { dateLocale, type Language, languages } from '@/i18n';
import type { FieldErrors } from '@/lib/api';

export type InviteStaffRequest = {
  email: string;
  role: string;
  language: Language;
};

export type SentInvite = { email: string; role: string; expiresAt: string };

/** What the API answered: the invite that was sent, or errors by field ("" for the whole form). */
export type InviteResult = { sent: SentInvite } | { errors: FieldErrors };

type Props = {
  /** The roles the logged-in inviter may give (D-063); the API enforces the same rule. */
  roles: string[];
  /** The email's language when the address has no account yet (D-065). */
  defaultLanguage: Language;
  onSubmit: (request: InviteStaffRequest) => Promise<InviteResult>;
};

// Named in their own language, like the header switch (D-065).
const languageNames: Record<Language, string> = { da: 'Dansk', en: 'English' };

function isLanguage(value: unknown): value is Language {
  return languages.includes(value as Language);
}

/**
 * Invites a person to a staff account by email (D-058). Only the roles the
 * inviter may give are offered: any role for an admin, Front desk and
 * Instructor for a manager (D-063).
 */
export function InviteStaffForm({ roles, defaultLanguage, onSubmit }: Props) {
  const { t } = useTranslation();
  const roleLabel = useId();
  const languageLabel = useId();
  const [role, setRole] = useState(roles.length === 1 ? roles[0] : '');
  const [language, setLanguage] = useState<Language>(defaultLanguage);
  const [errors, setErrors] = useState<FieldErrors>({});
  const [sent, setSent] = useState<SentInvite | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    const email = String(new FormData(formElement).get('email') ?? '').trim();
    const local: FieldErrors = {};
    if (!email) {
      local.email = [t('staffInvite.emailRequired')];
    }
    if (!role) {
      local.role = [t('staffInvite.roleRequired')];
    }
    if (Object.keys(local).length > 0) {
      setErrors(local);
      setSent(null);
      return;
    }

    setSubmitting(true);
    const result = await onSubmit({ email, role, language });
    setSubmitting(false);
    if ('sent' in result) {
      setErrors({});
      setSent(result.sent);
      formElement.reset();
      return;
    }
    setSent(null);
    setErrors(result.errors);
  }

  const expires = sent
    ? new Intl.DateTimeFormat(dateLocale(), {
        dateStyle: 'medium',
        timeStyle: 'short',
        timeZone: 'Europe/Copenhagen',
      }).format(new Date(sent.expiresAt))
    : '';

  return (
    <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-6">
      <p role="status" className="empty:hidden">
        {sent && (
          <span className="flex gap-2 rounded-md border border-border bg-muted p-3 text-sm">
            <CheckCircle2Icon
              aria-hidden="true"
              className="mt-0.5 size-4 shrink-0 text-success"
            />
            <span>
              {t('staffInvite.sent', {
                email: sent.email,
                role: t(`staffRoles.${sent.role}`),
                expires,
              })}
            </span>
          </span>
        )}
      </p>
      <FieldError id="invite-staff-error" messages={errors['']} />

      <div className="flex flex-col gap-2">
        <Label htmlFor="invite-email">{t('staffInvite.email')}</Label>
        <Input
          id="invite-email"
          name="email"
          type="email"
          autoComplete="off"
          required
          aria-invalid={errors.email ? true : undefined}
          aria-describedby={errors.email ? 'invite-email-error' : undefined}
        />
        <FieldError id="invite-email-error" messages={errors.email} />
      </div>

      <div className="flex flex-col gap-3">
        <p id={roleLabel} className="text-sm font-medium">
          {t('staffInvite.role')}
        </p>
        <RadioGroup
          value={role}
          onValueChange={(next) => setRole(String(next))}
          aria-labelledby={roleLabel}
          className="flex flex-col gap-3"
        >
          {roles.map((code) => (
            <div
              key={code}
              className="flex items-start gap-3 rounded-md border border-border p-3 has-aria-checked:border-primary"
            >
              <Radio
                id={`invite-role-${code}`}
                value={code}
                aria-describedby={`invite-role-${code}-hint`}
                className={cn(
                  'relative mt-0.5 flex size-5 shrink-0 cursor-pointer items-center justify-center rounded-full border border-input bg-card after:absolute after:-inset-3 aria-checked:border-primary',
                  focusRing,
                )}
              >
                <RadioIndicator className="size-2.5 rounded-full bg-primary" />
              </Radio>
              <div className="flex flex-col gap-1">
                <Label
                  htmlFor={`invite-role-${code}`}
                  className="cursor-pointer text-base"
                >
                  {t(`staffRoles.${code}`)}
                </Label>
                <span
                  id={`invite-role-${code}-hint`}
                  className="text-sm text-muted-foreground"
                >
                  {t(`staffInvite.roleHints.${code}`)}
                </span>
              </div>
            </div>
          ))}
        </RadioGroup>
        <FieldError id="invite-role-error" messages={errors.role} />
      </div>

      <div className="flex flex-col gap-2">
        <p id={languageLabel} className="text-sm font-medium">
          {t('staffInvite.language')}
        </p>
        <SegmentedControl
          value={language}
          onValueChange={(next) => isLanguage(next) && setLanguage(next)}
          aria-labelledby={languageLabel}
          className="flex self-start rounded-md border border-border-strong p-0.5"
        >
          {languages.map((code) => (
            <SegmentedControlItem
              key={code}
              value={code}
              lang={code}
              className={cn(
                'h-8 cursor-pointer rounded-sm px-3 text-sm text-muted-foreground hover:text-foreground aria-checked:bg-muted aria-checked:text-foreground',
                focusRing,
              )}
            >
              {languageNames[code]}
            </SegmentedControlItem>
          ))}
        </SegmentedControl>
        <p className="text-sm text-muted-foreground">
          {t('staffInvite.languageHint')}
        </p>
        <FieldError id="invite-language-error" messages={errors.language} />
      </div>

      <Button
        type="submit"
        disabled={submitting}
        className="w-full sm:w-auto sm:self-start"
      >
        {t('staffInvite.submit')}
      </Button>
    </form>
  );
}
