import { useState } from 'react'
import type { ReactNode } from 'react'

export function DrawingMenu({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState(true)
  return <aside className={`drawing-menu order-1 flex min-w-0 flex-col self-stretch overflow-hidden border border-[#c6d0be] bg-[#f6f8f1] lg:col-start-2 lg:row-start-1 lg:row-span-2 ${open ? 'w-full lg:w-72 xl:w-80' : 'w-full lg:w-12'}`} aria-label="Menu integrado do desenho">
    <button type="button" className="flex min-h-12 w-full !rounded-none items-center justify-between gap-3 border-0 bg-horta-forest px-3 py-3 text-left text-white" aria-expanded={open} aria-controls="drawing-menu-content" aria-label={open ? 'Recolher menu do desenho' : 'Abrir menu do desenho'} onClick={() => setOpen((current) => !current)}>
      <span aria-hidden="true" className="font-mono text-lg">☷</span>{open && <span className="flex-1"><strong className="block text-sm">Modo construção</strong><span className="block text-[10px] text-white/70">Terreno · setores · canteiros</span></span>}<span aria-hidden="true" className={open ? 'text-xs' : 'hidden'}>−</span>
    </button>
    <div id="drawing-menu-content" hidden={!open} className="min-h-0 max-h-[55dvh] flex-1 overflow-y-auto overscroll-contain lg:max-h-[calc(78dvh+100px)]">{children}</div>
  </aside>
}
