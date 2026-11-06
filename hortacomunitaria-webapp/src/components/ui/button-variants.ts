import { cva } from 'class-variance-authority';
export const buttonVariants = cva(
  "cursor-pointer group border border-transparent bg-transparent p-0 whitespace-nowrap focus-visible:outline-hidden inline-flex items-center justify-center has-data-[arrow=true]:justify-between text-sm font-medium ring-offset-white transition-[color,box-shadow] disabled:pointer-events-none disabled:opacity-60 [&_svg]:shrink-0",
  {
    variants: {
      variant: {

        clear:
          "bg-transparent text-red-600 hover:bg-red-50 hover:text-red-700 font-bold transition-colors",
        light: "bg-white border-horta-line text-horta-forest hover:bg-horta-soft",
        dark: "bg-black text-white hover:bg-neutral-800 focus:ring-2 focus:ring-offset-1 focus:ring-neutral-600",
        primary:
          "bg-horta-forest text-white hover:bg-horta-forest/90 data-[state=open]:bg-horta-forest/90",
        mono: "bg-zinc-950 text-white dark:bg-zinc-300 dark:text-black hover:bg-zinc-950/90 dark:hover:bg-zinc-300/90 data-[state=open]:bg-zinc-950/90 dark:data-[state=open]:bg-zinc-300/90",
        destructive:
          "bg-horta-danger text-white hover:bg-horta-danger/90 data-[state=open]:bg-horta-danger/90",
        secondary:
          "bg-horta-canvas text-horta-ink hover:bg-horta-canvas/90 data-[state=open]:bg-horta-canvas/90",
        outline:
          "bg-white text-horta-ink border border-horta-line hover:bg-horta-soft data-[state=open]:bg-horta-soft",
        dashed:
          "text-horta-ink border border-horta-line border-dashed bg-white hover:bg-horta-soft hover:text-horta-ink data-[state=open]:text-horta-ink",
        ghost:
          "text-horta-ink hover:bg-horta-soft hover:text-horta-ink data-[state=open]:bg-horta-soft data-[state=open]:text-horta-ink",
        dim: "text-horta-muted hover:text-horta-ink data-[state=open]:text-horta-ink",
        foreground: "",
        inverse: "",
      },
      appearance: {
        default: "",
        ghost: "",
      },
      underline: {
        solid: "",
        dashed: "",
      },
      underlined: {
        solid: "",
        dashed: "",
      },
      size: {
        lg: "h-10 rounded-md px-4 text-sm gap-1.5 [&_svg:not([class*=size-])]:size-4",
        md: "h-9 rounded-md px-3 text-sm gap-1.5 [&_svg:not([class*=size-])]:size-4",
        sm: "h-7 rounded-md px-2.5 gap-1.25 text-xs [&_svg:not([class*=size-])]:size-3.5",
        icon: "size-8.5 rounded-md [&_svg:not([class*=size-])]:size-4 shrink-0",
      },
      autoHeight: {
        true: "",
        false: "",
      },
      shape: {
        default: "",
        circle: "rounded-full",
      },
      mode: {
        default:
          "focus-visible:ring-2 focus-visible:ring-horta-focus focus-visible:ring-offset-2",
        icon: "focus-visible:ring-2 focus-visible:ring-horta-focus focus-visible:ring-offset-2 shrink-0",
        link: "text-horta-forest h-auto p-0 bg-transparent rounded-none hover:bg-transparent data-[state=open]:bg-transparent",
        input: `
          justify-start font-normal hover:bg-white [&_svg]:transition-colors [&_svg]:hover:text-horta-ink data-[state=open]:bg-white
          focus-visible:border-horta-focus focus-visible:outline-hidden focus-visible:ring-[3px] focus-visible:ring-horta-focus/30
          [[data-state=open]>&]:border-horta-focus [[data-state=open]>&]:outline-hidden [[data-state=open]>&]:ring-[3px]
          [[data-state=open]>&]:ring-horta-focus/30
          aria-invalid:border-horta-danger/60 aria-invalid:ring-horta-danger/10 dark:aria-invalid:border-horta-danger dark:aria-invalid:ring-horta-danger/20
          in-data-[invalid=true]:border-horta-danger/60 in-data-[invalid=true]:ring-horta-danger/10 dark:in-data-[invalid=true]:border-horta-danger dark:in-data-[invalid=true]:ring-horta-danger/20
        `,
      },
      placeholder: {
        true: "text-horta-muted",
        false: "",
      },
    },
    compoundVariants: [
      {
        variant: "ghost",
        mode: "default",
        className:
          "[&_svg:not([role=img]):not([class*=text-]):not([class*=opacity-])]:opacity-60",
      },
      {
        variant: "outline",
        mode: "default",
        className:
          "[&_svg:not([role=img]):not([class*=text-]):not([class*=opacity-])]:opacity-60",
      },
      {
        variant: "dashed",
        mode: "default",
        className:
          "[&_svg:not([role=img]):not([class*=text-]):not([class*=opacity-])]:opacity-60",
      },
      {
        variant: "secondary",
        mode: "default",
        className:
          "[&_svg:not([role=img]):not([class*=text-]):not([class*=opacity-])]:opacity-60",
      },
      {
        variant: "outline",
        mode: "input",
        className:
          "[&_svg:not([role=img]):not([class*=text-]):not([class*=opacity-])]:opacity-60",
      },
      {
        variant: "outline",
        mode: "icon",
        className:
          "[&_svg:not([role=img]):not([class*=text-]):not([class*=opacity-])]:opacity-60",
      },
      {
        size: "md",
        autoHeight: true,
        className: "h-auto min-h-8.5",
      },
      {
        size: "sm",
        autoHeight: true,
        className: "h-auto min-h-7",
      },
      {
        size: "lg",
        autoHeight: true,
        className: "h-auto min-h-10",
      },
      {
        variant: "primary",
        mode: "default",
        appearance: "default",
        className: "shadow-xs shadow-black/5",
      },
      {
        variant: "mono",
        mode: "default",
        appearance: "default",
        className: "shadow-xs shadow-black/5",
      },
      {
        variant: "secondary",
        mode: "default",
        appearance: "default",
        className: "shadow-xs shadow-black/5",
      },
      {
        variant: "outline",
        mode: "default",
        appearance: "default",
        className: "shadow-xs shadow-black/5",
      },
      {
        variant: "dashed",
        mode: "default",
        appearance: "default",
        className: "shadow-xs shadow-black/5",
      },
      {
        variant: "destructive",
        mode: "default",
        appearance: "default",
        className: "shadow-xs shadow-black/5",
      },
      {
        variant: "primary",
        mode: "icon",
        appearance: "default",
        className: "shadow-xs shadow-black/5",
      },
      {
        variant: "mono",
        mode: "icon",
        appearance: "default",
        className: "shadow-xs shadow-black/5",
      },
      {
        variant: "secondary",
        mode: "icon",
        appearance: "default",
        className: "shadow-xs shadow-black/5",
      },
      {
        variant: "outline",
        mode: "icon",
        appearance: "default",
        className: "shadow-xs shadow-black/5",
      },
      {
        variant: "dashed",
        mode: "icon",
        appearance: "default",
        className: "shadow-xs shadow-black/5",
      },
      {
        variant: "destructive",
        mode: "icon",
        appearance: "default",
        className: "shadow-xs shadow-black/5",
      },
      {
        variant: "primary",
        mode: "link",
        underline: "solid",
        className:
          "font-medium text-horta-forest hover:text-horta-forest/90 [&_svg:not([role=img]):not([class*=text-])]:opacity-60 hover:underline hover:underline-offset-4 hover:decoration-solid",
      },
      {
        variant: "primary",
        mode: "link",
        underline: "dashed",
        className:
          "font-medium text-horta-forest hover:text-horta-forest/90 [&_svg:not([role=img]):not([class*=text-])]:opacity-60 hover:underline hover:underline-offset-4 hover:decoration-dashed decoration-1",
      },
      {
        variant: "primary",
        mode: "link",
        underlined: "solid",
        className:
          "font-medium text-horta-forest hover:text-horta-forest/90 [&_svg:not([role=img]):not([class*=text-])]:opacity-60 underline underline-offset-4 decoration-solid",
      },
      {
        variant: "primary",
        mode: "link",
        underlined: "dashed",
        className:
          "font-medium text-horta-forest hover:text-horta-forest/90 [&_svg]:opacity-60 underline underline-offset-4 decoration-dashed decoration-1",
      },
      {
        variant: "inverse",
        mode: "link",
        underline: "solid",
        className:
          "font-medium text-inherit [&_svg:not([role=img]):not([class*=text-])]:opacity-60 hover:underline hover:underline-offset-4 hover:decoration-solid",
      },
      {
        variant: "inverse",
        mode: "link",
        underline: "dashed",
        className:
          "font-medium text-inherit [&_svg:not([role=img]):not([class*=text-])]:opacity-60 hover:underline hover:underline-offset-4 hover:decoration-dashed decoration-1",
      },
      {
        variant: "inverse",
        mode: "link",
        underlined: "solid",
        className:
          "font-medium text-inherit [&_svg:not([role=img]):not([class*=text-])]:opacity-60 underline underline-offset-4 decoration-solid",
      },
      {
        variant: "inverse",
        mode: "link",
        underlined: "dashed",
        className:
          "font-medium text-inherit [&_svg:not([role=img]):not([class*=text-])]:opacity-60 underline underline-offset-4 decoration-dashed decoration-1",
      },
      {
        variant: "foreground",
        mode: "link",
        underline: "solid",
        className:
          "font-medium text-horta-ink [&_svg:not([role=img]):not([class*=text-])]:opacity-60 hover:underline hover:underline-offset-4 hover:decoration-solid",
      },
      {
        variant: "foreground",
        mode: "link",
        underline: "dashed",
        className:
          "font-medium text-horta-ink [&_svg:not([role=img]):not([class*=text-])]:opacity-60 hover:underline hover:underline-offset-4 hover:decoration-dashed decoration-1",
      },
      {
        variant: "foreground",
        mode: "link",
        underlined: "solid",
        className:
          "font-medium text-horta-ink [&_svg:not([role=img]):not([class*=text-])]:opacity-60 underline underline-offset-4 decoration-solid",
      },
      {
        variant: "foreground",
        mode: "link",
        underlined: "dashed",
        className:
          "font-medium text-horta-ink [&_svg:not([role=img]):not([class*=text-])]:opacity-60 underline underline-offset-4 decoration-dashed decoration-1",
      },
      {
        variant: "primary",
        appearance: "ghost",
        className:
          "bg-transparent text-horta-forest/90 hover:bg-horta-forest/5 data-[state=open]:bg-horta-forest/5",
      },
      {
        variant: "destructive",
        appearance: "ghost",
        className:
          "bg-transparent text-horta-danger/90 hover:bg-horta-danger/5 data-[state=open]:bg-horta-danger/5",
      },
      {
        variant: "ghost",
        mode: "icon",
        className: "text-horta-muted",
      },
      {
        size: "sm",
        mode: "icon",
        className: "w-7 h-7 p-0 [&_svg:not([class*=size-])]:size-3.5",
      },
      {
        size: "md",
        mode: "icon",
        className: "w-8.5 h-8.5 p-0 [&_svg:not([class*=size-])]:size-4",
      },
      {
        size: "icon",
        className: "w-8.5 h-8.5 p-0 [&_svg:not([class*=size-])]:size-4",
      },
      {
        size: "lg",
        mode: "icon",
        className: "w-10 h-10 p-0 [&_svg:not([class*=size-])]:size-4",
      },
      {
        mode: "input",
        placeholder: true,
        variant: "outline",
        className: "font-normal text-horta-muted",
      },
      {
        mode: "input",
        variant: "outline",
        size: "sm",
        className: "gap-1.25",
      },
      {
        mode: "input",
        variant: "outline",
        size: "md",
        className: "gap-1.5",
      },
      {
        mode: "input",
        variant: "outline",
        size: "lg",
        className: "gap-1.5",
      },
    ],
    defaultVariants: {
      variant: "primary",
      mode: "default",
      size: "md",
      shape: "default",
      appearance: "default",
    },
  },
);
