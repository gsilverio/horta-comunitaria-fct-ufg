import { useState } from 'react'
import type { ReactNode } from 'react'
import { PanelRightClose, PanelRightOpen } from 'lucide-react'

/** Painel lateral contextual: etapas no topo e somente as ferramentas da etapa ativa. */
export function DrawingMenu({ steps, title, description, children }: { steps: ReactNode; title: string; description: string; children: ReactNode }) {
  const [open, setOpen] = useState(true)
  return <aside className={`drawing-menu order-2 flex min-w-0 flex-col self-stretch overflow-hidden border border-horta-line bg-white lg:col-start-2 lg:row-start-1 lg:h-0 lg:min-h-full ${open ? 'w-full lg:w-80' : 'w-full lg:w-12'}`} aria-label="Painel de construção do mapa">
    <div className={`hidden items-center justify-between gap-2 border-b lg:flex border-horta-line bg-[#f6f8f1] px-3 py-2 ${open ? '' : 'lg:flex-col lg:px-1'}`}>
      {open && <strong className="text-xs font-semibold uppercase tracking-wide text-horta-muted">Construção</strong>}
      <button type="button" className="interactive-icon-button hidden lg:inline-flex" aria-expanded={open} aria-controls="drawing-menu-content" title={open ? 'Recolher painel' : 'Abrir painel'} aria-label={open ? 'Recolher painel de construção' : 'Abrir painel de construção'} onClick={() => setOpen(current => !current)}>
        {open ? <PanelRightClose aria-hidden="true" /> : <PanelRightOpen aria-hidden="true" />}
      </button>
    </div>
    <div id="drawing-menu-content" hidden={!open} className="flex min-h-0 flex-1 flex-col">
      {steps}
      <div className="border-b border-horta-line px-4 py-3">
        <h2 className="m-0 text-base font-semibold text-horta-forest">{title}</h2>
        <p className="mt-1 mb-0 text-xs leading-relaxed text-horta-muted" aria-live="polite">{description}</p>
      </div>
      <div className="min-h-0 max-h-[55dvh] flex-1 overflow-y-auto overscroll-contain lg:max-h-none">{children}</div>
    </div>
  </aside>
}
