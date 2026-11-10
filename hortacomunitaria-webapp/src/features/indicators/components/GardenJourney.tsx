import { Icon } from '../../../components/Icon'
import { routeHref } from '../../../hooks/useRoute'
import type { Bed } from '../../organization/types/Bed'

interface GardenJourneyProps {
  garden: string
  sectors?: number
  beds?: Bed[]
  crops?: number
  hasLayout?: boolean
}

/** Atalhos do percurso horta → setor → canteiro → cultivo. Apenas conta registros já retornados pela API. */
export function GardenJourney({ garden, sectors, beds, crops, hasLayout }: GardenJourneyProps) {
  const pending = beds?.filter(bed => !bed.configuration || !bed.sectorAssignment).length ?? 0
  const known = (value?: number) => value === undefined ? '—' : String(value)
  const steps = [
    { label: 'Setores', value: known(sectors), detail: sectors === 0 ? 'Cadastre o primeiro setor' : 'Áreas ativas da horta', href: routeHref(garden, 'horta', 'setores'), pending: false },
    { label: 'Canteiros', value: known(beds?.length), detail: pending ? `${pending} com cadastro pendente` : beds?.length === 0 ? 'Abra um setor para cadastrar' : 'Área e setor informados', href: routeHref(garden, 'horta', 'canteiros'), pending: pending > 0 },
    { label: 'Cultivos', value: known(crops), detail: 'Em canteiros na data de hoje', href: routeHref(garden, 'cultivo', 'ciclos'), pending: false },
    { label: 'Mapa da horta', value: hasLayout === undefined ? '—' : hasLayout ? 'Salvo' : 'A desenhar', detail: hasLayout ? 'Abrir o modo interativo' : 'Desenhe terreno, setores e canteiros', href: routeHref(garden, 'horta', 'interativa'), pending: false },
  ]
  return <nav aria-label="Percurso da horta"><ol className="journey">
    {steps.map(step => <li key={step.label}><a href={step.href}>
      <span className="journey-label">{step.label}<Icon name="chevron" /></span>
      <span className="journey-value">{step.value}</span>
      <span className="journey-detail" data-pending={step.pending}>{step.detail}</span>
    </a></li>)}
  </ol></nav>
}
