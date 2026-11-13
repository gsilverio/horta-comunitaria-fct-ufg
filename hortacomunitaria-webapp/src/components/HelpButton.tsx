import type { ReactNode } from 'react'
import { CircleHelp } from 'lucide-react'
import { Dialog, DialogContent, DialogDescription, DialogTitle, DialogTrigger } from './ui/dialog'

/** Ícone "?" ao lado de um título: abre um diálogo curto com a explicação, mantendo a tela enxuta. */
export function HelpButton({ title, children }: { title: string; children: ReactNode }) {
  return <Dialog>
    <DialogTrigger asChild>
      <button type="button" className="help-button" aria-label={`Ajuda: ${title}`} title="Ajuda"><CircleHelp aria-hidden="true" /></button>
    </DialogTrigger>
    <DialogContent className="max-w-md gap-3 p-5">
      <DialogTitle className="m-0 pr-8 text-base font-semibold text-horta-forest">{title}</DialogTitle>
      <DialogDescription asChild><div className="help-content">{children}</div></DialogDescription>
    </DialogContent>
  </Dialog>
}
