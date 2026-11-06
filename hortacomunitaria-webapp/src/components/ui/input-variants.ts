import { cva } from 'class-variance-authority';
export const inputVariants = cva(
  'flex w-full bg-light-light rounded-md border border-horta-line text-sm ring-offset-0 ring-offset-white hover:border-gray-400 focus:ring-offset-0 focus:border-horta-forest placeholder:text-horta-muted focus-visible:outline-none focus-visible:ring-0 focus-visible:ring-horta-focus focus-visible:ring-offset-0 disabled:cursor-not-allowed disabled:opacity-50',
  {
    variants: {
      size: {
        default: 'h-10 px-3 py-2',
        sm: 'h-8 px-2 py-1',
        lg: 'h-11 px-4 py-3'
      }
    },
    defaultVariants: {
      size: 'default'
    }
  }
);
