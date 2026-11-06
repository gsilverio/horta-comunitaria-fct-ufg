import * as React from "react";
import { X } from "lucide-react";
import { cn } from "../libs/utils";

export const closeButtonToneClassName =
  "bg-rose-50 text-rose-600 hover:bg-rose-100 hover:text-rose-700";

export const CloseButton = React.forwardRef<
  HTMLButtonElement,
  React.ComponentPropsWithoutRef<"button">
>(({ className, ...props }, ref) => (
  <button
    ref={ref}
    type="button"
    aria-label="Fechar"
    title="Fechar"
    className={cn(
      "inline-flex size-7 shrink-0 cursor-pointer items-center justify-center rounded-md transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-horta-forest/30 disabled:pointer-events-none disabled:opacity-50",
      closeButtonToneClassName,
      className,
    )}
    {...props}
  >
    <X className="size-3.5 shrink-0" strokeWidth={2.25} />
  </button>
));

CloseButton.displayName = "CloseButton";
