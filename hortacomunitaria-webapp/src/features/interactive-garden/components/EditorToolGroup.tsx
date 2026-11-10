import type { ReactNode } from 'react'

/** Bloco do painel lateral com título curto. */
export function PanelSection({ title, children, tone = 'default' }: { title: string; children: ReactNode; tone?: 'default' | 'danger' }) {
  return <section className="border-b border-horta-line px-4 py-3 last:border-b-0" aria-label={title}>
    <h3 className={`mt-0 mb-2 text-[11px] font-semibold uppercase tracking-wide ${tone === 'danger' ? 'text-[#8d3430]' : 'text-horta-muted'}`}>{title}</h3>
    {children}
  </section>
}

/** Botão de ferramenta com ícone e legenda; `pressed` indica a ferramenta ativa no mapa. */
export function ToolTile({ label, ariaLabel, hint, icon, pressed, disabled, shortcut, onClick }: { label: string; /** Nome acessível estável quando difere da legenda visível. */ ariaLabel?: string; hint?: string; icon: ReactNode; pressed?: boolean; disabled?: boolean; shortcut?: string; onClick: () => void }) {
  return <button type="button" className={`interactive-tool-tile${pressed ? ' active' : ''}`} aria-label={ariaLabel} aria-pressed={pressed} disabled={disabled} onClick={onClick} title={shortcut ? `${ariaLabel ?? label} (${shortcut})` : ariaLabel ?? label} aria-keyshortcuts={shortcut}>
    <span aria-hidden="true" className="interactive-tool-tile-icon">{icon}</span>
    <span className="min-w-0"><strong>{label}</strong>{hint && <small>{hint}</small>}</span>
  </button>
}
