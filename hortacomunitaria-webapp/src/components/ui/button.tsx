import { buttonVariants } from './button-variants';
import * as React from "react";
import { type VariantProps } from "class-variance-authority";
import { ChevronDown, type LucideIcon } from "lucide-react";
import { Slot as SlotPrimitive } from "@radix-ui/react-slot";
import { cn } from "../libs/utils";



type ButtonProps = React.ComponentProps<"button"> &
  VariantProps<typeof buttonVariants> & {
    selected?: boolean;
    asChild?: boolean;
  };

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  (
    {
      className,
      selected,
      variant,
      shape,
      appearance,
      mode,
      size,
      autoHeight,
      underlined,
      underline,
      asChild = false,
      placeholder = false,
      ...props
    },
    ref,
  ) => {
    const Comp = asChild ? SlotPrimitive : "button";

    return (
      <Comp
        ref={ref}
        data-slot="button"
        className={cn(
          buttonVariants({
            variant,
            size,
            shape,
            appearance,
            mode,
            autoHeight,
            placeholder,
            underlined,
            underline,
            className,
          }),
          asChild && props.disabled && "pointer-events-none opacity-50",
        )}
        {...(selected && { "data-state": "open" })}
        {...props}
      />
    );
  },
);

Button.displayName = "Button";

interface ButtonArrowProps extends React.SVGProps<SVGSVGElement> {
  icon?: LucideIcon;
}

function ButtonArrow({
  icon: Icon = ChevronDown,
  className,
  ...props
}: ButtonArrowProps) {
  return (
    <Icon
      data-slot="button-arrow"
      className={cn("ms-auto -me-1", className)}
      {...props}
    />
  );
}

export { Button, ButtonArrow };
