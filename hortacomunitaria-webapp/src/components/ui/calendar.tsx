'use client';
import * as React from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { DayPicker } from 'react-day-picker';
import { format } from 'date-fns';
import { cn } from '../libs/utils';
import { buttonVariants } from './button-variants';

export type CalendarProps = React.ComponentProps<typeof DayPicker>;

function Calendar({ className, classNames, formatters, showOutsideDays = true, ...props }: CalendarProps) {
  const capitalize = (text: string) => text.charAt(0).toLocaleUpperCase() + text.slice(1);
  return (
    <DayPicker
      showOutsideDays={showOutsideDays}
      className={cn('p-3', className)}
      classNames={{
        months: 'flex flex-col sm:flex-row space-y-4 sm:space-x-4 sm:space-y-0',
        month: 'space-y-4',
        caption: 'flex justify-center pt-1 relative items-center gap-1',
        caption_label: 'text-sm font-medium h-7 flex items-center', // Ajustado para não sumir
        caption_dropdowns: 'flex justify-center gap-1 items-center z-10',
        nav: 'space-x-1 flex items-center',
        nav_button: cn(
          buttonVariants({ variant: 'outline' }),
          'h-7 w-7 bg-transparent p-0 opacity-50 hover:opacity-100'
        ),
        nav_button_previous: 'absolute left-1',
        nav_button_next: 'absolute right-1',
        table: 'w-full border-collapse space-y-1',
        head_row: 'flex',
        head_cell: 'text-horta-muted rounded-md w-9 font-normal text-[0.8rem] uppercase',
        row: 'flex w-full mt-2',
        cell: cn(
          'relative p-0 text-center text-sm focus-within:relative focus-within:z-20 [&:has([aria-selected])]:bg-horta-soft',
          props.mode === 'range'
            ? '[&:has(>.day-range-end)]:rounded-r-md [&:has(>.day-range-start)]:rounded-l-md first:[&:has([aria-selected])]:rounded-l-md last:[&:has([aria-selected])]:rounded-r-md'
            : 'rounded-md'
        ),
        day: cn(
          buttonVariants({ variant: 'ghost' }),
          'h-9 w-9 p-0 font-normal aria-selected:opacity-100'
        ),
        day_range_start: 'day-range-start bg-horta-forest text-white hover:bg-horta-forest hover:text-white focus:bg-horta-forest focus:text-white',
        day_range_end: 'day-range-end bg-horta-forest text-white hover:bg-horta-forest hover:text-white focus:bg-horta-forest focus:text-white',
        day_selected: 'bg-horta-forest text-white hover:bg-horta-forest hover:text-white focus:bg-horta-forest focus:text-white',
        day_today: 'bg-horta-soft text-horta-ink',
        day_outside: 'text-horta-muted opacity-50',
        day_disabled: 'text-horta-muted opacity-50',
        day_range_middle: 'aria-selected:bg-horta-soft aria-selected:text-horta-ink',
        day_hidden: 'invisible',
        dropdown: "rdp-dropdown border border-slate-200 rounded px-1 text-xs bg-white",
        dropdown_month: "relative inline-flex",
        dropdown_year: "relative inline-flex",
        ...classNames
      }}
      components={{
        IconLeft: () => <ChevronLeft className="h-4 w-4" />,
        IconRight: () => <ChevronRight className="h-4 w-4" />
      }}
      {...props}
      formatters={{
        formatCaption: (date, options) => capitalize(format(date, 'LLLL yyyy', options)),
        formatMonthCaption: (date, options) => capitalize(format(date, 'LLLL', options)),
        ...formatters
      }}
    />
  );
}
Calendar.displayName = 'Calendar';
export { Calendar };
