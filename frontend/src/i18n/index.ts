import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import da from './locales/da.json';
import en from './locales/en.json';

// Danish is the default and English is available (D-065).
export const languages = ['da', 'en'] as const;
export type Language = (typeof languages)[number];

const storageKey = 'language';

function isLanguage(value: unknown): value is Language {
  return languages.includes(value as Language);
}

// Browser storage can be missing or blocked (private windows), so the choice is
// only remembered when it can be.
function storedLanguage(): Language {
  try {
    const stored = localStorage.getItem(storageKey);
    return isLanguage(stored) ? stored : 'da';
  } catch {
    return 'da';
  }
}

i18n.on('languageChanged', (language) => {
  document.documentElement.lang = language;
  try {
    localStorage.setItem(storageKey, language);
  } catch {
    // Not remembered; the page still works in the chosen language.
  }
});

void i18n.use(initReactI18next).init({
  resources: { da: { translation: da }, en: { translation: en } },
  lng: storedLanguage(),
  fallbackLng: 'da',
  supportedLngs: languages,
  interpolation: { escapeValue: false },
});

/** The current UI language. */
export function currentLanguage(): Language {
  return i18n.resolvedLanguage === 'en' ? 'en' : 'da';
}

/**
 * The Intl locale for dates and times in the current language: Danish formats
 * (24-hour time, `dd.mm.yyyy`) in Danish, British English (24-hour time, day
 * before month) in English.
 */
export function dateLocale(): 'da-DK' | 'en-GB' {
  return currentLanguage() === 'en' ? 'en-GB' : 'da-DK';
}

export default i18n;
