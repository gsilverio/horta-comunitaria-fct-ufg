import { http } from "../../../services/http"
import type { Page } from "../../../types/api"
import type { AuditEntry } from "../../../types/records"
import type { Bed, BedValues } from "../types/Bed"
export interface BedFilters { page?: number; pageSize?: number; isActive?: boolean }
const path = (garden: string) => `/gardens/${encodeURIComponent(garden)}/beds`
export const bedApi = {
  list: async (garden: string, filters: BedFilters, signal: AbortSignal) => (await http.get<Page<Bed>>(`${path(garden)}/catalog`, { signal, params: filters })).data,
  get: async (garden: string, id: string, signal?: AbortSignal) => (await http.get<Bed>(`${path(garden)}/${encodeURIComponent(id)}`, { signal })).data,
  create: async (garden: string, values: BedValues) => (await http.post<Bed>(path(garden), { code: values.code, name: values.name, location: values.location })).data,
  correct: async (garden: string, id: string, values: BedValues, expectedVersion: number, reason: string) => (await http.put<Bed>(`${path(garden)}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  deactivate: async (garden: string, id: string, expectedVersion: number, reason: string) => (await http.post<Bed>(`${path(garden)}/${encodeURIComponent(id)}/inactivation`, { expectedVersion, reason })).data,
  history: async (garden: string, id: string, page: number, signal: AbortSignal) => (await http.get<Page<AuditEntry>>(`${path(garden)}/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data,
}
