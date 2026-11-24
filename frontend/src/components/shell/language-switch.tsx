import {
  MenuGroupLabel,
  MenuRadioGroup,
  MenuRadioItem,
} from '@ui-sdk/react/menu';
import {
  SegmentedControl,
  SegmentedControlItem,
} from '@ui-sdk/react/segmented-control';
import { cn } from 'cn';
import { useTranslation } from 'react-i18next';
import { focusRing } from '@/components/ui/button';
import { currentLanguage, type Language, languages } from '@/i18n';

// Each language is named in its own language, so a visitor who can't read the
// current one still finds theirs (D-065).
const names: Record<Language, string> = { da: 'Dansk', en: 'English' };

function useLanguage() {
  const { t, i18n } = useTranslation();
  const language = currentLanguage();
  // A radio group can report "nothing checked"; a language is always chosen.
  const change = (next: string | null) => {
    if (next) {
      void i18n.changeLanguage(next);
    }
  };
  return { t, language, change };
}

/** DA/EN switch in the header on wider screens (D-056, D-065). */
export function LanguageSwitch() {
  const { t, language, change } = useLanguage();

  return (
    <SegmentedControl
      value={language}
      onValueChange={change}
      aria-label={t('shell.language')}
      className="flex rounded-md border border-border-strong p-0.5"
    >
      {languages.map((code) => (
        <SegmentedControlItem
          key={code}
          value={code}
          aria-label={names[code]}
          lang={code}
          className={cn(
            'h-8 min-w-9 cursor-pointer rounded-sm px-2 text-xs font-medium text-muted-foreground uppercase hover:text-foreground aria-checked:bg-muted aria-checked:text-foreground',
            focusRing,
          )}
        >
          {code}
        </SegmentedControlItem>
      ))}
    </SegmentedControl>
  );
}

const menuRadioItem =
  'flex h-11 cursor-pointer items-center justify-between rounded-md px-3 text-base text-foreground outline-none data-highlighted:bg-muted aria-checked:font-medium';

/** The same choice inside the phone menu. */
export function LanguageMenuGroup() {
  const { t, language, change } = useLanguage();

  return (
    <MenuRadioGroup
      value={language}
      onValueChange={change}
      aria-label={t('shell.language')}
      className="flex flex-col gap-1"
    >
      <MenuGroupLabel className="px-3 pt-2 text-xs text-subtle">
        {t('shell.language')}
      </MenuGroupLabel>
      {languages.map((code) => (
        <MenuRadioItem
          key={code}
          value={code}
          lang={code}
          className={menuRadioItem}
        >
          {names[code]}
          <span aria-hidden="true" className="text-xs text-subtle uppercase">
            {code}
          </span>
        </MenuRadioItem>
      ))}
    </MenuRadioGroup>
  );
}
