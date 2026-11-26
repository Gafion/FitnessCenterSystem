import { type FormEvent, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { FieldError } from '@/features/account/field-error';
import type { FieldErrors, Schemas } from '@/lib/api';

type ClassType = Schemas['StaffClassType'];

/** Saved, or errors by field ("name", or "" for the whole form). */
export type ClassTypeResult = 'saved' | { errors: FieldErrors };

type Props = {
  classTypes: readonly ClassType[];
  onCreate: (name: string) => Promise<ClassTypeResult>;
  onRename: (id: string, name: string) => Promise<ClassTypeResult>;
};

/**
 * The class types (Spinning, Yoga, …) a class is scheduled from (D-062). A manager adds
 * one or renames one; the API keeps names unique ignoring case. Types can't be removed.
 */
export function ClassTypes({ classTypes, onCreate, onRename }: Props) {
  const { t } = useTranslation();
  return (
    <div className="flex flex-col gap-4">
      {classTypes.length === 0 ? (
        <p className="text-muted-foreground">{t('staffClasses.types.empty')}</p>
      ) : (
        <ul
          aria-label={t('staffClasses.types.title')}
          className="divide-y divide-border overflow-hidden rounded-lg border border-border bg-card"
        >
          {classTypes.map((type) => (
            <ClassTypeRow key={type.id} type={type} onRename={onRename} />
          ))}
        </ul>
      )}
      <NameForm
        id="new-class-type"
        label={t('staffClasses.types.newName')}
        submit={t('staffClasses.types.add')}
        onSubmit={onCreate}
        resetOnSave
      />
    </div>
  );
}

function ClassTypeRow({
  type,
  onRename,
}: {
  type: ClassType;
  onRename: Props['onRename'];
}) {
  const { t } = useTranslation();
  const [renaming, setRenaming] = useState(false);
  return (
    <li aria-label={type.name} className="flex flex-col gap-3 p-4">
      {renaming ? (
        <NameForm
          id={`rename-${type.id}`}
          label={t('staffClasses.types.renameLabel', { name: type.name })}
          submit={t('staffClasses.types.save')}
          defaultValue={type.name}
          onSubmit={(name) => onRename(type.id, name)}
          onClose={() => setRenaming(false)}
        />
      ) : (
        <div className="flex items-center justify-between gap-4">
          <span className="font-medium">{type.name}</span>
          <Button
            variant="outline"
            size="sm"
            aria-label={t('staffClasses.types.renameButton', {
              name: type.name,
            })}
            onClick={() => setRenaming(true)}
          >
            {t('staffClasses.types.rename')}
          </Button>
        </div>
      )}
    </li>
  );
}

function NameForm({
  id,
  label,
  submit,
  defaultValue,
  onSubmit,
  onClose,
  resetOnSave,
}: {
  id: string;
  label: string;
  submit: string;
  defaultValue?: string;
  onSubmit: (name: string) => Promise<ClassTypeResult>;
  onClose?: () => void;
  resetOnSave?: boolean;
}) {
  const { t } = useTranslation();
  const [errors, setErrors] = useState<FieldErrors>({});
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    const name = String(new FormData(formElement).get('name') ?? '').trim();
    if (!name) {
      setErrors({ name: [t('staffClasses.types.nameRequired')] });
      return;
    }
    setSubmitting(true);
    const result = await onSubmit(name);
    setSubmitting(false);
    if (result === 'saved') {
      setErrors({});
      if (resetOnSave) {
        formElement.reset();
      }
      onClose?.();
      return;
    }
    setErrors(result.errors);
  }

  const messages = [...(errors.name ?? []), ...(errors[''] ?? [])];
  return (
    <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-2">
      <Label htmlFor={id}>{label}</Label>
      <div className="flex flex-col gap-2 sm:flex-row">
        <Input
          id={id}
          name="name"
          maxLength={100}
          autoComplete="off"
          defaultValue={defaultValue}
          aria-invalid={messages.length > 0 ? true : undefined}
          aria-describedby={messages.length > 0 ? `${id}-error` : undefined}
        />
        <div className="flex gap-2">
          <Button type="submit" disabled={submitting} className="flex-1">
            {submit}
          </Button>
          {onClose && (
            <Button
              type="button"
              variant="outline"
              disabled={submitting}
              onClick={onClose}
              className="flex-1"
            >
              {t('staffClasses.form.close')}
            </Button>
          )}
        </div>
      </div>
      <FieldError id={`${id}-error`} messages={messages} />
    </form>
  );
}
