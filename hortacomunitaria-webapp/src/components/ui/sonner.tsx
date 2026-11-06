import { useTheme } from 'next-themes';
import { Toaster as Sonner } from 'sonner';

type ToasterProps = React.ComponentProps<typeof Sonner>;

const Toaster = ({ ...props }: ToasterProps) => {
  const { theme = 'system' } = useTheme();

  return (
    <Sonner
      theme={theme as ToasterProps['theme']}
      className="toaster group"
      toastOptions={{
        classNames: {
          toast:
            'group toast group-[.toaster]:bg-white group-[.toaster]:text-horta-ink group-[.toaster]:border-horta-line group-[.toaster]:shadow-lg',
          description: 'group-[.toast]:text-horta-muted',
          actionButton: 'group-[.toast]:bg-horta-forest group-[.toast]:text-white',
          cancelButton: 'group-[.toast]:bg-horta-canvas group-[.toast]:text-horta-muted'
        }
      }}
      {...props}
    />
  );
};

export { Toaster };
