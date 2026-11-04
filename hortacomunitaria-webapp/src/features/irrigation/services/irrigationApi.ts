import { http } from '../../../services/http'
import type { Page } from '../../../types/api'
import type { Bed, CatalogItem, Consumption, Filters, HistoryEntry, IrrigationDetail, IrrigationSummary, IrrigationValues, Participant } from '../types/irrigation'

const gardenPath = (gardenId: string) => `/gardens/${encodeURIComponent(gardenId)}`
async function catalog<T>(path: string, signal: AbortSignal, params: Record<string, string> = {}): Promise<T[]> {
  const items: T[] = []
  let page = 1
  while (!signal.aborted) {
    const { data } = await http.get<Page<T>>(path, { signal, params: { ...params, page, pageSize: 100 } })
    items.push(...data.items)
    if (!data.hasNextPage) return items
    page++
  }
  signal.throwIfAborted()
  return items
}
export const irrigationApi = {
  gardens: (signal: AbortSignal) => catalog<CatalogItem>('/gardens', signal),
  sectors: (garden: string, signal: AbortSignal) => catalog<CatalogItem>(`${gardenPath(garden)}/sectors`, signal),
  participants: (garden: string, signal: AbortSignal) => catalog<Participant>(`${gardenPath(garden)}/participants`, signal),
  beds: (garden: string, sectorId: string, on: string, signal: AbortSignal) => catalog<Bed>(`${gardenPath(garden)}/beds`, signal, { sectorId, on }),
  async list(garden: string, filters: Filters, page: number, signal: AbortSignal) {
    return (await http.get<Page<IrrigationSummary>>(`${gardenPath(garden)}/irrigations`, { signal, params: { ...filters, sectorId: filters.sectorId || undefined, page, pageSize: 10 } })).data
  },
  async consumption(garden: string, filters: Filters, page: number, signal: AbortSignal) {
    return (await http.get<Consumption>(`${gardenPath(garden)}/indicators/water-consumption`, { signal, params: { from: filters.from, to: filters.to, sectorId: filters.sectorId || undefined, page, pageSize: 5 } })).data
  },
  async detail(garden: string, id: string, signal?: AbortSignal) {
    return (await http.get<IrrigationDetail>(`${gardenPath(garden)}/irrigations/${encodeURIComponent(id)}`, { signal })).data
  },
  async history(garden: string, id: string, page: number, signal: AbortSignal) {
    return (await http.get<Page<HistoryEntry>>(`${gardenPath(garden)}/irrigations/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data
  },
  async create(garden: string, values: IrrigationValues, operationId: string) {
    return (await http.post<IrrigationDetail>(`${gardenPath(garden)}/irrigations`, { ...values, operationId })).data
  },
  async correct(garden: string, id: string, values: IrrigationValues, expectedVersion: number, reason: string) {
    return (await http.put<IrrigationDetail>(`${gardenPath(garden)}/irrigations/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data
  },
  async invalidate(garden: string, id: string, expectedVersion: number, reason: string) {
    return (await http.post<IrrigationDetail>(`${gardenPath(garden)}/irrigations/${encodeURIComponent(id)}/invalidation`, { expectedVersion, reason })).data
  },
}
