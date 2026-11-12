import { linkTo } from '../../../routes/routes'
import { useCanManage } from '../../auth/hooks/useAuth'
import type { Bed } from '../types/Bed'

/** Atalho para o próximo passo do canteiro: completar o cadastro ou seguir para o cultivo. */
export function BedRowAction({ garden, bed, sectorId, writable }: { garden: string; bed: Bed; sectorId?: string; writable: boolean }) {
  const canManage = useCanManage()
  if (!writable || !bed.isActive) return null
  const context = sectorId ? { sectorId } : undefined
  if (!bed.configuration || !bed.sectorAssignment) return canManage && <a className="organization-row-action underline underline-offset-4" aria-label={`Completar cadastro: ${bed.name}`} href={linkTo(garden, 'horta/canteiros', bed.id, new URLSearchParams(context), 'configurar')}>Completar cadastro</a>
  return <a className="organization-row-action underline underline-offset-4" aria-label={`Adicionar cultivo: ${bed.name}`} href={linkTo(garden, 'horta/canteiros', bed.id, undefined, 'cultivar')}>Adicionar cultivo</a>
}
