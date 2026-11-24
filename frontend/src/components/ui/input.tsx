import { Input as InputPrimitive } from '@ui-sdk/react/input';
import { cn } from 'cn';
import type * as React from 'react';
import { focusRing } from './button';

/**
 * A UI SDK text input in the Fjord style. Its border uses --input (the subtle
 * text colour), which passes WCAG 1.4.11's 3:1; design.md's hairline would not.
 */
function Input({
  className,
  ...props
}: React.ComponentProps<typeof InputPrimitive> & { className?: string }) {
  return (
    <InputPrimitive
      className={cn(
        'h-11 w-full min-w-0 rounded-md border border-input bg-card px-3 text-base text-foreground transition-colors placeholder:text-subtle md:h-10 md:text-sm',
        focusRing,
        'focus-visible:border-ring',
        'data-disabled:cursor-not-allowed data-disabled:bg-muted data-disabled:text-muted-foreground',
        'aria-invalid:border-destructive aria-invalid:focus-visible:ring-destructive',
        className,
      )}
      {...props}
    />
  );
}

export { Input };
