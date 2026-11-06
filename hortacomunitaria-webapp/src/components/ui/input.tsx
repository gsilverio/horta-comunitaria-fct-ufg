import { inputVariants } from './input-variants';
import * as React from 'react';
import { type VariantProps } from 'class-variance-authority';
import { cn } from '../libs/utils';

// Define input size variants


export interface InputProps
  extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'size'>, // Omit native `size`
    VariantProps<typeof inputVariants> {}

const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, type, size, ...props }, ref) => {
    return (
      <input type={type} className={cn(inputVariants({ size }), className)} ref={ref} {...props} />
    );
  }
);

Input.displayName = 'Input';

export { Input };
