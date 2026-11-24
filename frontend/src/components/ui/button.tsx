import { Button as ButtonPrimitive } from '@ui-sdk/react/button';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from 'cn';
import type * as React from 'react';

/** Focus ring shared by every interactive control (design.md, elevation level 4). */
export const focusRing =
  'outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background';

const buttonVariants = cva(
  [
    'inline-flex shrink-0 cursor-pointer items-center justify-center gap-2 rounded-md border border-transparent font-medium whitespace-nowrap transition-colors select-none',
    focusRing,
    // Disabled and pending buttons look the same in every variant: a flat
    // surface-2 fill with muted text, so a disabled action (a full class's Book
    // button) never reads as a faded but working one.
    'data-disabled:cursor-not-allowed data-disabled:border-border data-disabled:bg-muted data-disabled:text-muted-foreground',
    "[&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
  ],
  {
    variants: {
      variant: {
        default:
          'bg-primary text-primary-foreground hover:not-data-disabled:bg-primary/90',
        outline:
          'border-border-strong bg-card text-foreground hover:not-data-disabled:bg-muted',
        secondary:
          'bg-secondary text-secondary-foreground hover:not-data-disabled:bg-[color-mix(in_oklch,var(--secondary),var(--foreground)_6%)]',
        ghost: 'text-foreground hover:not-data-disabled:bg-muted',
        destructive:
          'border-destructive/40 bg-card text-destructive hover:not-data-disabled:bg-destructive/10',
        link: 'text-primary underline-offset-4 hover:underline data-disabled:border-transparent data-disabled:bg-transparent',
      },
      size: {
        // 44 px on phones (touch target), 40 px from md up (design.md).
        default: 'h-11 px-4 text-sm md:h-10',
        sm: 'h-9 px-3 text-sm',
        lg: 'h-12 px-5 text-base',
        icon: 'size-11 md:size-10',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  },
);

type ButtonProps = React.ComponentProps<typeof ButtonPrimitive> &
  VariantProps<typeof buttonVariants> & { className?: string };

/**
 * A UI SDK button in the Fjord style. Pass a router `<Link />` to `render` to
 * make a link that looks like a button.
 */
function Button({ className, variant, size, ...props }: ButtonProps) {
  return (
    <ButtonPrimitive
      data-variant={variant ?? 'default'}
      className={cn(buttonVariants({ variant, size }), className)}
      {...props}
    />
  );
}

export { Button, buttonVariants };
