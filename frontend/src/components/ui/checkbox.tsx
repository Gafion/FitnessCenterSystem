import {
  CheckboxIndicator,
  Checkbox as CheckboxPrimitive,
} from '@ui-sdk/react/checkbox';
import { cn } from 'cn';
import { CheckIcon } from 'lucide-react';
import type * as React from 'react';
import { focusRing } from './button';

/** A UI SDK checkbox in the Fjord style; its border passes 3:1 (--input). */
function Checkbox({
  className,
  ...props
}: React.ComponentProps<typeof CheckboxPrimitive> & { className?: string }) {
  return (
    <CheckboxPrimitive
      className={cn(
        // The ::after widens the touch area without moving the layout.
        'relative flex size-5 shrink-0 cursor-pointer items-center justify-center rounded-sm border border-input bg-card transition-colors after:absolute after:-inset-3',
        focusRing,
        'data-checked:border-primary data-checked:bg-primary data-checked:text-primary-foreground',
        'data-disabled:cursor-not-allowed data-disabled:bg-muted',
        'aria-invalid:border-destructive',
        className,
      )}
      {...props}
    >
      <CheckboxIndicator className="grid place-content-center">
        <CheckIcon aria-hidden="true" className="size-3.5" strokeWidth={3} />
      </CheckboxIndicator>
    </CheckboxPrimitive>
  );
}

export { Checkbox };
