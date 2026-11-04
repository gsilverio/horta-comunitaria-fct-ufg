import { http } from "../../../services/http"
import type { Page } from "../../../types/api"
import type { AuditEntry } from "../../../types/records"
import type { Cultivar, CultivarValues } from "../types/Cultivar"
export interface CultivarFilters { page?: number; pageSize?: number; isActive?: boolean }
const path = (parent: string) => `/species/${encodeURIComponent(parent)}/cultivars`
export const cultivarApi = {
  list: async (parent: string, filters: CultivarFilters, signal: AbortSignal) => (await http.get<Page<Cultivar>>(path(parent), { signal, params: filters })).data,
  get: async (parent: string, id: string, signal?: AbortSignal) => (await http.get<Cultivar>(`${path(parent)}/${encodeURIComponent(id)}`, { signal })).data,
  create: async (parent: string, values: CultivarValues) => (await http.post<Cultivar>(path(parent), { name: values.name, description: values.description })).data,
  correct: async (parent: string, id: string, values: CultivarValues, expectedVersion: number, reason: string) => (await http.put<Cultivar>(`${path(parent)}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  deactivate: async (parent: string, id: string, expectedVersion: number, reason: string) => (await http.post<Cultivar>(`${path(parent)}/${encodeURIComponent(id)}/inactivation`, { expectedVersion, reason })).data,
  history: async (parent: string, id: string, page: number, signal: AbortSignal) => (await http.get<Page<AuditEntry>>(`${path(parent)}/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data,
}
