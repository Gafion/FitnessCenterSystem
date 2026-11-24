import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from 'cn';
import type * as React from 'react';

// A status pill (design.md "rounded.pill"), such as Booked, Full or "2 spots
// left". UI SDK has no badge, as it has no behaviour; this is a plain <span>.
// Each colour passes WCAG AA on cards (FIT-96 contrast table).
const badgeVariants = cva(
  'inline-flex h-6 shrink-0 items-center gap-1 rounded-full border px-2.5 text-xs font-medium whitespace-nowrap',
  {
    variants: {
      variant: {
        default: 'border-primary/30 bg-primary/10 text-primary',
        neutral: 'border-border bg-muted text-muted-foreground',
        success: 'border-success/30 bg-success/10 text-success',
        danger: 'border-destructive/30 bg-destructive/10 text-destructive',
      },
    },
    defaultVariants: { variant: 'default' },
  },
);

function Badge({
  className,
  variant,
  ...props
}: React.ComponentProps<'span'> & VariantProps<typeof badgeVariants>) {
  return (
    <span
      data-slot="badge"
      className={cn(badgeVariants({ variant }), className)}
      {...props}
    />
  );
}

export { Badge };
