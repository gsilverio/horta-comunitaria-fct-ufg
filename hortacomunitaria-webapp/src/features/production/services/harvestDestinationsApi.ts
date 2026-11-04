import { allPages } from '../../../services/pagination'
import type { DryingLot } from '../types/DryingLot'
export const harvestDestinationsApi = {
  list: async (garden: string, harvest: string, signal: AbortSignal) => (await allPages<DryingLot>(`/gardens/${encodeURIComponent(garden)}/drying-lots`, signal, { from: '0001-01-01', to: '9998-12-31', validity: 'Valid' })).filter(lot => lot.items.some(item => item.harvestId === harvest)),
}
