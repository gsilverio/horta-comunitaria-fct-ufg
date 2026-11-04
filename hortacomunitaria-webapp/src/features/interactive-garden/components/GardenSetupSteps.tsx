export type GardenSetupStage = 'terrain' | 'sectors' | 'beds'

interface GardenSetupStepsProps {
  stage: GardenSetupStage
  hasTerrain: boolean
  sectorCount: number
  bedCount: number
  canEditSectors: boolean
  canPlaceBeds: boolean
  selectTerrain: () => void
  selectSectors: () => void
  selectBeds: () => void
}

export function GardenSetupSteps({ stage, hasTerrain, sectorCount, bedCount, canEditSectors, canPlaceBeds, selectTerrain, selectSectors, selectBeds }: GardenSetupStepsProps) {
  const steps = [
    { id: 'terrain', title: 'Delimitar terreno', description: 'Desenhe o contorno e defina o espaço da horta.', detail: hasTerrain ? 'Contorno definido' : 'Comece pelo contorno', complete: hasTerrain, enabled: true, select: selectTerrain },
    { id: 'sectors', title: 'Desenhar setores', description: 'Divida o terreno em áreas de cultivo.', detail: sectorCount ? `${sectorCount} ${sectorCount === 1 ? 'setor delimitado' : 'setores delimitados'}` : 'Aguardando o terreno', complete: sectorCount > 0, enabled: canEditSectors, select: selectSectors },
    { id: 'beds', title: 'Adicionar canteiros', description: 'Distribua canteiros e mandalas nos setores.', detail: bedCount ? `${bedCount} ${bedCount === 1 ? 'canteiro no mapa' : 'canteiros no mapa'}` : 'Aguardando os setores', complete: bedCount > 0, enabled: canPlaceBeds, select: selectBeds },
  ]
  const current = Math.max(0, steps.findIndex((step) => step.id === stage))
  const guidance = {
    terrain: 'Marque os vértices no mapa e conclua o contorno para continuar.',
    sectors: 'Com o terreno definido, desenhe os setores onde ficarão os canteiros.',
    beds: 'Escolha um formato de canteiro ou gere uma mandala para organizar o cultivo.',
  }
  return <nav className="mb-5 rounded-2xl border border-horta-line bg-white p-4 sm:p-6" aria-label="Etapas do mapa">
    <div className="mb-5 flex items-start justify-between gap-4">
      <div><span className="eyebrow">Organização do mapa</span><h2 className="mt-1 text-lg font-semibold text-horta-forest">Monte sua horta</h2><p className="mt-1 text-sm text-horta-muted">Do terreno ao cultivo, uma etapa de cada vez.</p></div>
      <span className="shrink-0 rounded-full bg-horta-soft px-3 py-1.5 text-xs font-semibold text-horta-forest">Etapa {current + 1} de 3</span>
    </div>
    <ol className="grid gap-3 md:grid-cols-3">
      {steps.map((step, index) => {
        const active = step.id === stage
        const status = active ? 'Em edição' : step.complete ? 'Definido' : step.enabled ? 'Disponível' : 'Aguardando'
        return <li key={step.id} className="min-w-0">
          <button type="button" aria-current={active ? 'step' : undefined} disabled={!step.enabled} onClick={step.select}
            className={`group flex h-full w-full flex-col rounded-xl border p-4 text-left transition focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-horta-forest disabled:cursor-not-allowed ${active ? 'border-horta-forest bg-horta-soft/60 ring-1 ring-horta-forest' : step.enabled ? 'border-horta-line bg-white hover:border-horta-forest hover:bg-horta-soft/30' : 'border-horta-line bg-[#f8f9f6]'}`}>
            <span className="mb-3 flex w-full items-center justify-between gap-2">
              <span aria-hidden="true" className={`flex size-9 shrink-0 items-center justify-center rounded-full text-sm font-bold ${active ? 'bg-horta-forest text-white' : step.complete ? 'bg-horta-soft text-horta-forest' : 'border border-horta-line bg-white text-horta-muted'}`}>
                {step.complete && !active ? <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m5 12 4 4L19 6" /></svg> : String(index + 1).padStart(2, '0')}
              </span>
              <span className={`rounded-full px-2.5 py-1 text-[11px] font-semibold ${active ? 'bg-horta-forest text-white' : step.complete ? 'bg-horta-soft text-horta-forest' : 'text-horta-muted'}`}>{status}</span>
            </span>
            <strong className="text-sm font-semibold text-horta-forest">{step.title}</strong>
            <span className="mt-1 text-xs leading-relaxed text-horta-muted">{step.description}</span>
            <span className="mt-auto flex w-full items-center justify-between gap-2 pt-4 text-xs font-medium text-horta-forest">
              <span>{step.enabled && !step.complete ? active ? 'Etapa atual' : 'Pronto para começar' : step.detail}</span>
              {step.enabled && <span aria-hidden="true" className="transition-transform group-hover:translate-x-1">→</span>}
            </span>
          </button>
        </li>
      })}
    </ol>
    <p className="mt-4 flex gap-2 border-t border-horta-line pt-4 text-xs leading-relaxed text-horta-muted" aria-live="polite"><span aria-hidden="true" className="font-semibold text-horta-forest">{current + 1}.</span>{guidance[stage]}</p>
  </nav>
}
