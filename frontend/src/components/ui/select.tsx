import {
  Select,
  SelectContent,
  SelectItem,
  SelectItemIndicator,
  SelectItemText,
  SelectLabel,
  SelectTrigger,
  SelectValue,
} from '@ui-sdk/react/select';
import { cn } from 'cn';
import { CheckIcon, ChevronDownIcon } from 'lucide-react';
import { focusRing } from './button';

export type PickerOption = { value: string; label: string };

type Props = {
  id: string;
  label: string;
  placeholder: string;
  options: readonly PickerOption[];
  value: string;
  onValueChange: (value: string) => void;
  invalid?: boolean;
  describedBy?: string;
};

/**
 * Picks one option from a list in a popup: a UI SDK select in the Fjord style, sized
 * like `Input` (44 px on phones, 40 px from md up). The trigger is a combobox named
 * by its label.
 */
export function Picker({
  id,
  label,
  placeholder,
  options,
  value,
  onValueChange,
  invalid,
  describedBy,
}: Props) {
  return (
    <Select
      value={value || null}
      onValueChange={(next) => onValueChange(next ? String(next) : '')}
    >
      <div className="flex flex-col gap-2">
        <SelectLabel className="text-sm leading-none font-medium select-none">
          {label}
        </SelectLabel>
        <SelectTrigger
          id={id}
          aria-invalid={invalid ? true : undefined}
          aria-describedby={describedBy}
          className={cn(
            'flex h-11 w-full min-w-0 cursor-pointer items-center justify-between gap-2 rounded-md border border-input bg-card px-3 text-left text-base text-foreground transition-colors md:h-10 md:text-sm',
            focusRing,
            'aria-invalid:border-destructive',
          )}
        >
          <SelectValue
            placeholder={placeholder}
            className="truncate data-placeholder:text-subtle"
          />
          <ChevronDownIcon
            aria-hidden="true"
            className="size-4 shrink-0 text-subtle"
          />
        </SelectTrigger>
      </div>
      <SelectContent className="z-50 max-h-72 min-w-(--anchor-width) overflow-y-auto rounded-md border border-border-strong bg-popover p-1 text-popover-foreground">
        {options.map((option) => (
          <SelectItem
            key={option.value}
            value={option.value}
            label={option.label}
            className="flex cursor-pointer items-center justify-between gap-2 rounded-sm px-2 py-2 text-sm outline-none data-highlighted:bg-muted"
          >
            <SelectItemText>{option.label}</SelectItemText>
            <SelectItemIndicator>
              <CheckIcon aria-hidden="true" className="size-4 text-primary" />
            </SelectItemIndicator>
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
