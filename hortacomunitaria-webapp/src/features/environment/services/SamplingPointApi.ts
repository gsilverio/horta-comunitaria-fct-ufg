import { http } from "../../../services/http"
import type { Page } from "../../../types/api"
import type { AuditEntry } from "../../../types/records"
import type { SamplingPoint, SamplingPointValues } from "../types/SamplingPoint"
export interface SamplingPointFilters { page?: number; pageSize?: number; isActive?: boolean }
const path = (garden: string) => `/gardens/${encodeURIComponent(garden)}/sampling-points`
export const samplingPointApi = {
  list: async (garden: string, filters: SamplingPointFilters, signal: AbortSignal) => (await http.get<Page<SamplingPoint>>(path(garden), { signal, params: filters })).data,
  get: async (garden: string, id: string, signal?: AbortSignal) => (await http.get<SamplingPoint>(`${path(garden)}/${encodeURIComponent(id)}`, { signal })).data,
  create: async (garden: string, values: SamplingPointValues) => (await http.post<SamplingPoint>(path(garden), { code: values.code, description: values.description, location: values.location, bedId: values.bedId, sectorId: values.sectorId })).data,
  correct: async (garden: string, id: string, values: SamplingPointValues, expectedVersion: number, reason: string) => (await http.put<SamplingPoint>(`${path(garden)}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  deactivate: async (garden: string, id: string, expectedVersion: number, reason: string) => (await http.post<SamplingPoint>(`${path(garden)}/${encodeURIComponent(id)}/inactivation`, { expectedVersion, reason })).data,
  history: async (garden: string, id: string, page: number, signal: AbortSignal) => (await http.get<Page<AuditEntry>>(`${path(garden)}/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data,
}
