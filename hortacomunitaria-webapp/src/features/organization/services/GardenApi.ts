import { http } from "../../../services/http"
import type { Page } from "../../../types/api"
import type { AuditEntry } from "../../../types/records"
import type { Garden, GardenValues } from "../types/Garden"
export interface GardenFilters { page?: number; pageSize?: number; isActive?: boolean }
const path = () => '/gardens'
export const gardenApi = {
  list: async (filters: GardenFilters, signal: AbortSignal) => (await http.get<Page<Garden>>(`${path()}/catalog`, { signal, params: filters })).data,
  get: async (id: string, signal?: AbortSignal) => (await http.get<Garden>(`${path()}/${encodeURIComponent(id)}`, { signal })).data,
  create: async (values: GardenValues) => (await http.post<Garden>(path(), { code: values.code, name: values.name, location: values.location, institutionalAreaM2: values.institutionalAreaM2, operationalAreaM2: values.operationalAreaM2, experimentalAreaM2: values.experimentalAreaM2 })).data,
  correct: async (id: string, values: GardenValues, expectedVersion: number, reason: string) => (await http.put<Garden>(`${path()}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  deactivate: async (id: string, expectedVersion: number, reason: string) => (await http.post<Garden>(`${path()}/${encodeURIComponent(id)}/inactivation`, { expectedVersion, reason })).data,
  history: async (id: string, page: number, signal: AbortSignal) => (await http.get<Page<AuditEntry>>(`${path()}/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data,
}
