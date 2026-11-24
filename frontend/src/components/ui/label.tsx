import { Label as LabelPrimitive } from '@ui-sdk/react/label';
import { cn } from 'cn';
import type * as React from 'react';

/** A UI SDK label: names any control by `htmlFor`, including a checkbox. */
function Label({
  className,
  ...props
}: React.ComponentProps<typeof LabelPrimitive> & { className?: string }) {
  return (
    <LabelPrimitive
      className={cn(
        'flex items-center gap-2 text-sm leading-none font-medium select-none',
        className,
      )}
      {...props}
    />
  );
}

export { Label };
