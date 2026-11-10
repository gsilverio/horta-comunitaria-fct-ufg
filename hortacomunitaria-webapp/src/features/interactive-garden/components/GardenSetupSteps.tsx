import { Check, Lock } from 'lucide-react'

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

/** Etapas como abas: definem qual conjunto de ferramentas aparece no painel lateral. */
export function GardenSetupSteps({ stage, hasTerrain, sectorCount, bedCount, canEditSectors, canPlaceBeds, selectTerrain, selectSectors, selectBeds }: GardenSetupStepsProps) {
  const steps = [
    { id: 'terrain', title: 'Terreno', detail: hasTerrain ? 'Contorno definido' : 'Comece pelo contorno', complete: hasTerrain, enabled: true, select: selectTerrain },
    { id: 'sectors', title: 'Setores', detail: sectorCount ? `${sectorCount} ${sectorCount === 1 ? 'setor' : 'setores'}` : canEditSectors ? 'Pronto para começar' : 'Conclua o terreno', complete: sectorCount > 0, enabled: canEditSectors, select: selectSectors },
    { id: 'beds', title: 'Canteiros', detail: bedCount ? `${bedCount} ${bedCount === 1 ? 'canteiro' : 'canteiros'}` : canPlaceBeds ? 'Pronto para começar' : 'Crie um setor', complete: bedCount > 0, enabled: canPlaceBeds, select: selectBeds },
  ]
  return <nav aria-label="Etapas do mapa">
    <ol className="m-0 grid list-none grid-cols-3 gap-0 border-b border-horta-line p-0">
      {steps.map((step, index) => {
        const active = step.id === stage
        return <li key={step.id} className="min-w-0">
          <button type="button" aria-current={active ? 'step' : undefined} disabled={!step.enabled} onClick={step.select}
            title={step.detail}
            className={`relative flex min-h-12 w-full items-center gap-2 rounded-none border-0 bg-transparent px-2 py-2 text-left font-normal text-horta-ink shadow-none hover:!bg-[#f3f6ef] disabled:cursor-not-allowed ${active ? 'after:absolute after:inset-x-0 after:-bottom-px after:h-0.5 after:bg-horta-forest' : ''}`}>
            <span aria-hidden="true" className={`flex size-6 shrink-0 items-center justify-center rounded-full text-xs font-semibold ${active ? 'bg-horta-forest text-white' : step.complete ? 'bg-horta-soft text-horta-forest' : 'border border-horta-line bg-white text-horta-muted'}`}>
              {!step.enabled ? <Lock className="size-3" /> : step.complete && !active ? <Check className="size-3.5" strokeWidth={2.6} /> : index + 1}
            </span>
            <strong className={`min-w-0 flex-1 text-sm ${active ? 'font-semibold text-horta-forest' : 'font-medium'}`}>{step.title}<span className="sr-only">: {step.detail}</span></strong>
          </button>
        </li>
      })}
    </ol>
  </nav>
}
