import { http } from "../../../services/http"
import type { Page } from "../../../types/api"
import type { AuditEntry } from "../../../types/records"
import type { Species, SpeciesValues } from "../types/Species"
export interface SpeciesFilters { page?: number; pageSize?: number; isActive?: boolean }
const path = () => '/species'
export const speciesApi = {
  list: async (filters: SpeciesFilters, signal: AbortSignal) => (await http.get<Page<Species>>(path(), { signal, params: filters })).data,
  get: async (id: string, signal?: AbortSignal) => (await http.get<Species>(`${path()}/${encodeURIComponent(id)}`, { signal })).data,
  create: async (values: SpeciesValues) => (await http.post<Species>(path(), { commonName: values.commonName, scientificName: values.scientificName, description: values.description })).data,
  correct: async (id: string, values: SpeciesValues, expectedVersion: number, reason: string) => (await http.put<Species>(`${path()}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  deactivate: async (id: string, expectedVersion: number, reason: string) => (await http.post<Species>(`${path()}/${encodeURIComponent(id)}/inactivation`, { expectedVersion, reason })).data,
  history: async (id: string, page: number, signal: AbortSignal) => (await http.get<Page<AuditEntry>>(`${path()}/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data,
}
