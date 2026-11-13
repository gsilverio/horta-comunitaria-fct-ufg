import { Plus, Wrench } from 'lucide-react'
import { linkTo } from '../../../routes/routes'
import { useCanManage } from '../../auth/hooks/useAuth'
import type { Bed } from '../types/Bed'

/** Atalho para o próximo passo do canteiro: completar o cadastro ou seguir para o cultivo. */
export function BedRowAction({ garden, bed, sectorId, writable }: { garden: string; bed: Bed; sectorId?: string; writable: boolean }) {
  const canManage = useCanManage()
  if (!writable || !bed.isActive) return null
  const context = sectorId ? { sectorId } : undefined
  if (!bed.configuration || !bed.sectorAssignment) return canManage && <a className="org-row-quick" aria-label={`Completar cadastro: ${bed.name}`} href={linkTo(garden, 'horta/canteiros', bed.id, new URLSearchParams(context), 'configurar')}><Wrench aria-hidden="true" />Completar cadastro</a>
  return <a className="org-row-quick" aria-label={`Adicionar cultivo: ${bed.name}`} href={linkTo(garden, 'horta/canteiros', bed.id, undefined, 'cultivar')}><Plus aria-hidden="true" />Adicionar cultivo</a>
}
