import { allPages } from '../../../services/pagination'
import type { IrrigationSector } from '../types/IrrigationSector'
import { bedApi } from './BedApi'
import { bedContextApi } from './bedContextApi'
export const organizationSetupApi = {
  async get(garden: string, page: number, on: string, signal: AbortSignal) {
    const [sectors, catalog] = await Promise.all([
      allPages<IrrigationSector>(`/gardens/${encodeURIComponent(garden)}/sectors/catalog`, signal, { isActive: true }),
      bedApi.list(garden, { isActive: true, page, pageSize: 10 }, signal),
    ])
    // The catalog identifies beds; only the dated detail resolves their effective area/sector.
    const items = await Promise.all(catalog.items.map(bed => bedContextApi.on(garden, bed.id, on, signal)))
    return { sectors, beds: { ...catalog, items } }
  },
}
