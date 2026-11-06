'use client';

import React from 'react';
import { type DialogProps } from '@radix-ui/react-dialog';
import { Command as CommandPrimitive } from 'cmdk';
import { Check, type LucideIcon, Search } from 'lucide-react';
import { cn } from '../libs/utils';
import { Dialog, DialogContent, DialogTitle } from './dialog';

function Command({
  className,
  ...props
}: React.ComponentProps<typeof CommandPrimitive>) {
  return (
    <CommandPrimitive
      className={cn(
        'flex h-full w-full flex-col overflow-hidden rounded-md bg-white text-horta-ink',
        className,
      )}
      {...props}
    />
  );
}

type CommandDialogProps = DialogProps & { className?: string };

const CommandDialog = ({
  children,
  className,
  ...props
}: CommandDialogProps) => {
  return (
    <Dialog {...props}>
      <DialogContent className={cn('overflow-hidden p-0 shadow-lg', className)}>
        <DialogTitle className="hidden" />
        <Command className="[&_[cmdk-group-heading]]:px-2 [&_[cmdk-group-heading]]:font-medium [&_[cmdk-group-heading]]:text-horta-muted [&_[cmdk-group]:not([hidden])_~[cmdk-group]]:pt-0 [&_[cmdk-group]]:px-2 [&_[cmdk-input-wrapper]_svg]:h-5 [&_[cmdk-input-wrapper]_svg]:w-5 [&_[cmdk-input]]:h-12 [&_[cmdk-item]]:px-2 [&_[cmdk-item]]:py-3 [&_[cmdk-item]_svg]:h-5 [&_[cmdk-item]_svg]:w-5">
          {children}
        </Command>
      </DialogContent>
    </Dialog>
  );
};

function CommandInput({
  className,
  ...props
}: React.ComponentProps<typeof CommandPrimitive.Input>) {
  return (
    <div
      className="flex items-center border-horta-line border-b px-3"
      cmdk-input-wrapper=""
      data-slot="command-input"
    >
      <Search className="me-2 h-4 w-4 shrink-0 opacity-50" />
      <CommandPrimitive.Input
        className={cn(
          'flex h-11 w-full rounded-md bg-transparent py-3 text-sm outline-hidden text-horta-ink placeholder:text-horta-muted disabled:cursor-not-allowed disabled:opacity-50',
          className,
        )}
        {...props}
      />
    </div>
  );
}

function CommandList({
  className,
  ...props
}: React.ComponentProps<typeof CommandPrimitive.List>) {
  return (
    <CommandPrimitive.List
      data-slot="command-list"
      className={cn(
        'max-h-[300px] overflow-y-auto overflow-x-hidden',
        className,
      )}
      {...props}
    />
  );
}

function CommandEmpty({
  ...props
}: React.ComponentProps<typeof CommandPrimitive.Empty>) {
  return (
    <CommandPrimitive.Empty
      data-slot="command-empty"
      className="py-6 text-center text-sm"
      {...props}
    />
  );
}

function CommandGroup({
  className,
  ...props
}: React.ComponentProps<typeof CommandPrimitive.Group>) {
  return (
    <CommandPrimitive.Group
      data-slot="command-group"
      className={cn(
        'overflow-hidden p-1.5 text-horta-ink [&_[cmdk-group-heading]]:px-2 [&_[cmdk-group-heading]]:py-1.5 [&_[cmdk-group-heading]]:text-xs [&_[cmdk-group-heading]]:font-medium [&_[cmdk-group-heading]]:text-horta-muted',
        className,
      )}
      {...props}
    />
  );
}

function CommandSeparator({
  className,
  ...props
}: React.ComponentProps<typeof CommandPrimitive.Separator>) {
  return (
    <CommandPrimitive.Separator
      data-slot="command-separator"
      className={cn('-mx-1.5 h-px bg-horta-line', className)}
      {...props}
    />
  );
}

function CommandItem({
  className,
  ...props
}: React.ComponentProps<typeof CommandPrimitive.Item>) {
  return (
    <CommandPrimitive.Item
      data-slot="command-item"
      className={cn(
        'relative flex text-horta-ink cursor-default gap-2 select-none items-center rounded-sm px-2 py-1.5 text-sm outline-hidden data-[disabled=true]:pointer-events-none data-[selected=true]:bg-horta-soft data-[disabled=true]:opacity-50 [&_svg]:pointer-events-none [&_svg]:size-4 [&_svg]:shrink-0',
        className,
      )}
      {...props}
    />
  );
}

const CommandShortcut = ({
  className,
  ...props
}: React.HTMLAttributes<HTMLSpanElement>) => {
  return (
    <span
      data-slot="command-shortcut"
      className={cn(
        'ms-auto text-xs tracking-widest text-horta-muted',
        className,
      )}
      {...props}
    />
  );
};

interface ButtonArrowProps extends React.SVGProps<SVGSVGElement> {
  icon?: LucideIcon; // Allows passing any Lucide icon
}

function CommandCheck({
  icon: Icon = Check,
  className,
  ...props
}: ButtonArrowProps) {
  return (
    <Icon
      data-slot="command-check"
      data-check="true"
      className={cn('size-4 ms-auto text-horta-forest', className)}
      {...props}
    />
  );
}

export {
  Command,
  CommandCheck,
  CommandDialog,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandSeparator,
  CommandShortcut,
};
