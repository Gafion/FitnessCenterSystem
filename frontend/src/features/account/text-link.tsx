import { Link, type LinkComponentProps } from '@tanstack/react-router';
import { cn } from 'cn';
import { focusRing } from '@/components/ui/button';

/** An inline link in the accent colour (design.md: "link emphasis"). */
export function TextLink({ className, ...props }: LinkComponentProps<'a'>) {
  return (
    <Link
      className={cn(
        'rounded-sm font-medium text-primary underline underline-offset-4 hover:no-underline',
        focusRing,
        className,
      )}
      {...props}
    />
  );
}
