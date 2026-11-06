'use client';

import * as React from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { Check, Minus } from 'lucide-react';
import * as CheckboxPrimitive from '@radix-ui/react-checkbox';
import { cn } from '../libs/utils';

// Define the variants for the Checkbox using cva.
const checkboxVariants = cva(
  `
    group peer bg-white shrink-0 rounded-none border-2 border-[#767676] ring-offset-white focus-visible:outline-none
    focus-visible:ring-2 focus-visible:ring-horta-focus focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50
    aria-invalid:border-horta-danger/60 aria-invalid:ring-horta-danger/10 dark:aria-invalid:border-horta-danger dark:aria-invalid:ring-horta-danger/20
    [[data-invalid=true]_&]:border-horta-danger/60 [[data-invalid=true]_&]:ring-horta-danger/10 dark:[[data-invalid=true]_&]:border-horta-danger dark:[[data-invalid=true]_&]:ring-horta-danger/20
    data-[state=checked]:bg-horta-forest data-[state=checked]:border-horta-forest data-[state=checked]:text-white data-[state=indeterminate]:bg-horta-forest data-[state=indeterminate]:border-horta-forest data-[state=indeterminate]:text-white
    [&_svg]:!text-white
    `,
  {
    variants: {
      size: {
        sm: 'size-4 [&_svg]:size-3',
        md: 'size-5 [&_svg]:size-3.5',
        lg: 'size-5.5 [&_svg]:size-4',
      },
    },
    defaultVariants: {
      size: 'sm',
    },
  },
);

function Checkbox({
  className,
  size,
  ...props
}: React.ComponentProps<typeof CheckboxPrimitive.Root> &
  VariantProps<typeof checkboxVariants>) {
  return (
    <CheckboxPrimitive.Root
      data-slot="checkbox"
      className={cn(checkboxVariants({ size }), className)}
      {...props}
    >
      <CheckboxPrimitive.Indicator
        className={cn('flex items-center justify-center text-current')}
      >
        <Check className="group-data-[state=indeterminate]:hidden" />
        <Minus className="hidden group-data-[state=indeterminate]:block" />
      </CheckboxPrimitive.Indicator>
    </CheckboxPrimitive.Root>
  );
}

export { Checkbox };
