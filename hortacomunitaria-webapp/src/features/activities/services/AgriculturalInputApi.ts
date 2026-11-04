import { http } from "../../../services/http"
import type { Page } from "../../../types/api"
import type { AuditEntry } from "../../../types/records"
import type { AgriculturalInput, AgriculturalInputValues } from "../types/AgriculturalInput"
export interface AgriculturalInputFilters { page?: number; pageSize?: number; isActive?: boolean }
const path = () => '/inputs'
export const agriculturalInputApi = {
  list: async (filters: AgriculturalInputFilters, signal: AbortSignal) => (await http.get<Page<AgriculturalInput>>(path(), { signal, params: filters })).data,
  get: async (id: string, signal?: AbortSignal) => (await http.get<AgriculturalInput>(`${path()}/${encodeURIComponent(id)}`, { signal })).data,
  create: async (values: AgriculturalInputValues) => (await http.post<AgriculturalInput>(path(), { name: values.name, category: values.category, referenceUnit: values.referenceUnit })).data,
  correct: async (id: string, values: AgriculturalInputValues, expectedVersion: number, reason: string) => (await http.put<AgriculturalInput>(`${path()}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  deactivate: async (id: string, expectedVersion: number, reason: string) => (await http.post<AgriculturalInput>(`${path()}/${encodeURIComponent(id)}/inactivation`, { expectedVersion, reason })).data,
  history: async (id: string, page: number, signal: AbortSignal) => (await http.get<Page<AuditEntry>>(`${path()}/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data,
}
