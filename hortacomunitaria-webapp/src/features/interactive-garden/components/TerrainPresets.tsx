import { useId, useState } from 'react'

export type TerrainPreset = 'rectangle' | 'square' | 'circle' | 'l-shape'

const presets = [
  { id: 'rectangle', label: 'Retangular', path: 'M2 6H30V22H2Z', caption: 'Linhas amplas' },
  { id: 'square', label: 'Quadrado', path: 'M5 3H27V25H5Z', caption: 'Espaço equilibrado' },
  { id: 'circle', label: 'Circular', path: 'M11 2H21V5H26V10H29V18H26V23H21V26H11V23H6V18H3V10H6V5H11Z', caption: 'Cultivo ao redor' },
  { id: 'l-shape', label: 'Em L', path: 'M3 3H15V15H29V25H3Z', caption: 'Novos caminhos' },
] as const

export function TerrainPresets({ select, compact = false }: { select: (preset: TerrainPreset) => void; compact?: boolean }) {
  const [chosen, setChosen] = useState<TerrainPreset>()
  const id = useId()
  return <section className={compact ? 'w-full' : 'mb-5 rounded-xl border-2 border-[#c5cfb7] bg-[#eef1e5] p-3 sm:p-4'} aria-label="Formatos prontos do terreno">
    {!compact && <header className="mb-3 flex flex-wrap items-center justify-between gap-2">
      <div><span className="font-mono text-[10px] font-bold uppercase tracking-[0.2em] text-[#667351]">Modo construção</span><h3 className="mt-1 text-sm font-bold text-horta-forest">Escolha seu terreno</h3></div>
      <span className="rounded border border-[#c8d3bc] bg-white/70 px-2 py-1 font-mono text-[10px] text-[#58694e]">4 formatos · edição livre</span>
    </header>}
    <div className={compact ? 'grid grid-cols-2 gap-2' : 'grid grid-cols-2 gap-3 sm:grid-cols-4'}>
      {presets.map((preset, index) => {
        const active = chosen === preset.id
        const clip = id + '-' + preset.id
        return <button key={preset.id} type="button" aria-label={preset.label} aria-pressed={active} onClick={() => { setChosen(preset.id); select(preset.id) }}
          className={compact ? `group flex min-h-11 w-full flex-col items-center gap-0.5 rounded-md border px-1 py-1.5 text-center text-horta-ink !shadow-none focus-visible:outline-2 focus-visible:outline-horta-forest ${active ? 'border-horta-forest bg-[#e6efdc]' : 'border-horta-line bg-white hover:!bg-[#f1f5ec]'}` : `group relative flex min-w-0 flex-col overflow-hidden rounded-lg border-2 text-left transition duration-150 focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-horta-forest motion-safe:hover:-translate-y-1 active:translate-y-0 ${active ? 'border-[#315e42] bg-[#f1f8df] shadow-[0_4px_0_#315e42]' : 'border-[#b5c3a4] bg-[#fafbf5] shadow-[0_4px_0_#b5c3a4] hover:border-[#638053]'}`}>
          <span className={compact ? "hidden" : "absolute top-2 left-2 z-10 font-mono text-[10px] font-bold text-[#71805e]"}>0{index + 1}</span>
          {active && !compact && <span className="absolute top-2 right-2 z-10 flex size-5 items-center justify-center rounded bg-[#315e42] text-xs text-white" aria-hidden="true">✓</span>}
          <span className={compact ? 'flex w-full items-center justify-center' : `flex w-full items-center justify-center border-b border-[#d8e1cb] px-3 pt-3 pb-1 ${active ? 'bg-[#dfecc8]' : 'bg-[#eaf0df] group-hover:bg-[#e1ebd2]'}`}>
            <svg className={compact ? 'h-9 w-12' : 'h-24 w-full max-w-40 sm:h-28'} viewBox="0 0 96 72" aria-hidden="true" shapeRendering="crispEdges">
              <defs><clipPath id={clip}><path d={preset.path} /></clipPath></defs>
              <ellipse cx="48" cy="59" rx="31" ry="6" fill="#344e2d" opacity=".12" />
              <g transform="translate(19 17) scale(1.8 1.1) skewY(-15)">
                <path d={preset.path} transform="translate(0 7)" fill="#886344" stroke="#624b35" strokeWidth="1.2" />
                <path d={preset.path} transform="translate(0 4)" fill="#b18a5b" stroke="#886344" strokeWidth="1.2" />
                <path d={preset.path} fill={active ? '#8fb956' : '#a5c676'} stroke="#52703e" strokeWidth="1.2" />
                <g clipPath={`url(#${clip})`}>
                  {Array.from({ length: 24 }, (_, i) => <path key={i} d={`M${3 + i % 6 * 5} ${5 + Math.floor(i / 6) * 6}h2v1h-2z`} fill={i % 3 === 0 ? '#d1e3a4' : '#6c9449'} opacity=".8" />)}
                  <path d="M0 14H32M16 0V28" fill="none" stroke="#e1edc4" strokeWidth=".4" opacity=".6" />
                </g>
              </g>
            </svg>
          </span>
          <span className={compact ? 'flex min-w-0 flex-col' : 'flex w-full flex-col gap-1 p-3'}><strong className="text-xs font-bold text-horta-forest sm:text-sm">{preset.label}</strong><span className={compact ? 'hidden' : 'text-[10px] text-[#71805e] sm:text-xs'}>{preset.caption}</span><span className={`${compact ? "hidden" : "mt-1"} font-mono text-[9px] font-bold uppercase tracking-wider ${active ? 'text-[#315e42]' : 'text-[#89947d]'}`}>{active ? 'Base selecionada' : 'Selecionar formato'}</span></span>
        </button>
      })}
    </div>
    {!compact && <p className="mt-4 mb-0 text-[11px] text-[#667351]">Escolha uma base ou desenhe no grid. Você pode mover, adicionar e remover os nós depois.</p>}
  </section>
}
