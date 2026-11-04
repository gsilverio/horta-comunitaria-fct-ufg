import { http } from '../../../services/http'
import type { Page } from '../../../types/api'
import { bedApi } from './BedApi'
import { bedContextApi } from './bedContextApi'

export const organizationBrowseApi = {
  async sectorBeds(garden: string, sectorId: string, page: number, on: string, signal: AbortSignal) {
    const { data } = await http.get<Page<{ id: string }>>(`/gardens/${encodeURIComponent(garden)}/beds`, { signal, params: { sectorId, on, page, pageSize: 10 } })
    const items = await Promise.all(data.items.map(item => bedContextApi.on(garden, item.id, on, signal)))
    return { ...data, items }
  },
  async beds(garden: string, page: number, isActive: boolean | undefined, on: string, signal: AbortSignal) {
    const data = await bedApi.list(garden, { page, pageSize: 10, isActive }, signal)
    const items = await Promise.all(data.items.map(item => bedContextApi.on(garden, item.id, on, signal)))
    return { ...data, items }
  },
}
