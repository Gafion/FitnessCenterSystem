import { AlertCircleIcon } from 'lucide-react';

export function FieldError({
  id,
  messages,
}: {
  id: string;
  messages?: string[];
}) {
  if (!messages?.length) {
    return null;
  }

  return (
    <p id={id} className="flex gap-1.5 text-sm text-destructive">
      <AlertCircleIcon aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
      {messages.join(' ')}
    </p>
  );
}
