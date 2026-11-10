import { routeHref } from '../../../hooks/useRoute'
import type { Bed } from '../types/Bed'

/** Atalho para o próximo passo do canteiro: completar o cadastro ou seguir para o cultivo. */
export function BedRowAction({ garden, bed, sectorId, writable }: { garden: string; bed: Bed; sectorId?: string; writable: boolean }) {
  if (!writable || !bed.isActive) return null
  const context = sectorId ? { sectorId } : undefined
  if (!bed.configuration || !bed.sectorAssignment) return <a className="organization-row-action underline underline-offset-4" aria-label={`Completar cadastro: ${bed.name}`} href={routeHref(garden, 'horta', 'canteiros', bed.id, new URLSearchParams({ ...context, setup: '1' }))}>Completar cadastro</a>
  return <a className="organization-row-action underline underline-offset-4" aria-label={`Adicionar cultivo: ${bed.name}`} href={routeHref(garden, 'horta', 'canteiros', bed.id, new URLSearchParams({ cultivate: '1' }))}>Adicionar cultivo</a>
}
