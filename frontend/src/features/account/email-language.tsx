import {
  SegmentedControl,
  SegmentedControlItem,
} from '@ui-sdk/react/segmented-control';
import { cn } from 'cn';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { focusRing } from '@/components/ui/button';
import { type Language, languages } from '@/i18n';

// Named in their own language, like the header switch (D-065).
const names: Record<Language, string> = { da: 'Dansk', en: 'English' };

function isLanguage(value: unknown): value is Language {
  return languages.includes(value as Language);
}

/**
 * The language of the account's emails (D-065). Signup sets it from the app's
 * language; here the person can change it.
 */
export function EmailLanguage({
  value,
  onChange,
}: {
  value: string;
  onChange: (language: Language) => Promise<void>;
}) {
  const { t } = useTranslation();
  const [language, setLanguage] = useState(isLanguage(value) ? value : 'da');
  const [status, setStatus] = useState<'idle' | 'saved' | 'failed'>('idle');

  async function change(next: string | null) {
    if (!isLanguage(next) || next === language) {
      return;
    }
    const previous = language;
    setLanguage(next);
    try {
      await onChange(next);
      setStatus('saved');
    } catch {
      setLanguage(previous);
      setStatus('failed');
    }
  }

  return (
    <div className="flex flex-col gap-2">
      <p id="email-language" className="text-sm text-muted-foreground">
        {t('account.emailLanguage.label')}
      </p>
      <SegmentedControl
        value={language}
        onValueChange={(next) => void change(next)}
        aria-labelledby="email-language"
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
            {names[code]}
          </SegmentedControlItem>
        ))}
      </SegmentedControl>
      <p role="status" className="text-sm text-muted-foreground">
        {status === 'saved' && t('account.emailLanguage.saved')}
      </p>
      {status === 'failed' && (
        <p role="alert" className="text-sm text-destructive">
          {t('account.emailLanguage.failed')}
        </p>
      )}
    </div>
  );
}
