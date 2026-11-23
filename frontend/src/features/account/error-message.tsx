import { useTranslation } from 'react-i18next';
import { FieldError } from './field-error';

/** Shows the message for an `error` code from the address bar, e.g. after a Google login. */
export function ErrorMessage({ code }: { code?: string }) {
  const { t } = useTranslation();
  if (!code) {
    return null;
  }

  return (
    <FieldError
      id="page-error"
      messages={[t(`errors.${code}`, { defaultValue: t('errors.unexpected') })]}
    />
  );
}
