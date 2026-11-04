import type { ReactNode } from 'react'

export function EditorToolGroup({ number, title, description, active, locked, children }: { number: string; title: string; description: string; active: boolean; locked: boolean; children: ReactNode }) {
  return <section className={`min-w-0 border-b border-[#d5ddce] ${active ? 'bg-[#edf3e6]' : 'bg-transparent'}`} aria-label={`Ferramentas de ${title.toLowerCase()}`}>
    <header className="flex items-start gap-2 px-3 pt-4 pb-2">
      <span className={`flex size-6 shrink-0 items-center justify-center font-mono text-xs font-bold ${active ? 'bg-horta-forest text-white' : 'bg-[#e6ecd9] text-horta-forest'}`}>{number}</span>
      <div className="min-w-0"><h3 className="m-0 text-sm font-bold text-horta-forest">{title}</h3><p className="mt-1 mb-0 text-[11px] leading-relaxed text-horta-muted">{description}</p></div>
      <span className="ml-auto shrink-0 pt-1 font-mono text-[9px] uppercase tracking-wide text-horta-muted">{locked ? 'Bloqueado' : active ? 'Em uso' : 'Disponível'}</span>
    </header>
    <div className="flex flex-col items-stretch gap-1 px-3 pb-4 [&>button]:w-full [&>button]:rounded-none [&>button]:!border-0 [&>button]:!shadow-none [&>button]:text-left [&>button]:text-xs">{children}</div>
  </section>
}
