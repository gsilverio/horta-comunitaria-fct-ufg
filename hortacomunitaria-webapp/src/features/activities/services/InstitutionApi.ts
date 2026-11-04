import { http } from "../../../services/http"
import type { Page } from "../../../types/api"
import type { AuditEntry } from "../../../types/records"
import type { Institution, InstitutionValues } from "../types/Institution"
export interface InstitutionFilters { page?: number; pageSize?: number; isActive?: boolean }
const path = () => '/institutions'
export const institutionApi = {
  list: async (filters: InstitutionFilters, signal: AbortSignal) => (await http.get<Page<Institution>>(path(), { signal, params: filters })).data,
  get: async (id: string, signal?: AbortSignal) => (await http.get<Institution>(`${path()}/${encodeURIComponent(id)}`, { signal })).data,
  create: async (values: InstitutionValues) => (await http.post<Institution>(path(), { name: values.name, acronym: values.acronym, type: values.type })).data,
  correct: async (id: string, values: InstitutionValues, expectedVersion: number, reason: string) => (await http.put<Institution>(`${path()}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  deactivate: async (id: string, expectedVersion: number, reason: string) => (await http.post<Institution>(`${path()}/${encodeURIComponent(id)}/inactivation`, { expectedVersion, reason })).data,
  history: async (id: string, page: number, signal: AbortSignal) => (await http.get<Page<AuditEntry>>(`${path()}/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data,
}
