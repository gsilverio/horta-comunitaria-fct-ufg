import { Lock } from 'lucide-react'
import { linkTo } from '../../../routes/routes'
import { Icon } from '../../../components/Icon'
import type { Bed } from '../../organization/types/Bed'

interface GardenJourneyProps {
  garden: string
  sectors?: number
  beds?: Bed[]
  crops?: number
  /** Mapa salvo com algum conteúdo (terreno ou objetos). */
  hasLayout?: boolean
  /** Contorno do terreno salvo no mapa. */
  hasTerrain?: boolean
}

/**
 * Atalhos do percurso mapa → setor → canteiro → cultivo. O mapa vem primeiro porque o terreno é a premissa:
 * numa horta nova (sem terreno e sem setores) os demais atalhos ficam bloqueados. Apenas conta registros já
 * retornados pela API.
 */
export function GardenJourney({ garden, sectors, beds, crops, hasLayout, hasTerrain }: GardenJourneyProps) {
  const pending = beds?.filter(bed => !bed.configuration || !bed.sectorAssignment).length ?? 0
  const known = (value?: number) => value === undefined ? '—' : String(value)
  const locked = hasTerrain === false && sectors === 0
  const steps = [
    { label: 'Mapa da horta', value: hasLayout === undefined ? '—' : hasLayout ? 'Salvo' : 'A desenhar', detail: hasLayout ? 'Abrir o modo interativo' : 'Comece desenhando o terreno', href: linkTo(garden, 'horta/mapa'), pending: false, locked: false },
    { label: 'Setores', value: known(sectors), detail: sectors === 0 ? 'Cadastre o primeiro setor' : 'Áreas ativas da horta', href: linkTo(garden, 'horta/setores'), pending: false, locked },
    { label: 'Canteiros', value: known(beds?.length), detail: pending ? `${pending} com cadastro pendente` : beds?.length === 0 ? 'Abra um setor para cadastrar' : 'Área e setor informados', href: linkTo(garden, 'horta/canteiros'), pending: pending > 0, locked },
    { label: 'Cultivos', value: known(crops), detail: 'Em canteiros na data de hoje', href: linkTo(garden, 'cultivo/ciclos'), pending: false, locked },
  ]
  return <nav aria-label="Percurso da horta"><ol className="journey">
    {steps.map(step => <li key={step.label}>{step.locked
      ? <div className="journey-locked" aria-disabled="true">
        <span className="journey-label">{step.label}<Lock aria-hidden="true" className="size-3.5" /></span>
        <span className="journey-value">{step.value}</span>
        <span className="journey-detail">Desenhe o terreno primeiro</span>
      </div>
      : <a href={step.href}>
        <span className="journey-label">{step.label}<Icon name="chevron" /></span>
        <span className="journey-value">{step.value}</span>
        <span className="journey-detail" data-pending={step.pending}>{step.detail}</span>
      </a>}</li>)}
  </ol></nav>
}
