import { gardenLayoutApi } from './gardenLayoutApi'
import { allPages } from '../../../services/pagination'
import { bedContextApi } from '../../organization/services/bedContextApi'
import type { Bed } from '../../organization/types/Bed'
import type { IrrigationSector } from '../../organization/types/IrrigationSector'
import type { InteractiveOrganization } from '../projection'

/** Leitura combinada para a projeção: mantém a relação horta → setor → canteiro do CRUD. */
export const interactiveGardenApi = {
  async get(garden: string, on: string, signal: AbortSignal): Promise<InteractiveOrganization> {
    const [sectors, catalog, savedLayout] = await Promise.all([
      allPages<IrrigationSector>(`/gardens/${encodeURIComponent(garden)}/sectors/catalog`, signal, { isActive: true }),
      allPages<Bed>(`/gardens/${encodeURIComponent(garden)}/beds/catalog`, signal, { isActive: true }),
      gardenLayoutApi.get(garden, signal),
    ])
    const beds = await Promise.all(catalog.map((bed) => bedContextApi.on(garden, bed.id, on, signal)))
    return { sectors, beds, savedLayout }
  },
}
