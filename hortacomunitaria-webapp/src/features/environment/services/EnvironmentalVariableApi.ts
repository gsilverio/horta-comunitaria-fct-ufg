import { http } from "../../../services/http"
import type { Page } from "../../../types/api"
import type { AuditEntry } from "../../../types/records"
import type { EnvironmentalVariable, EnvironmentalVariableValues } from "../types/EnvironmentalVariable"
export interface EnvironmentalVariableFilters { page?: number; pageSize?: number; isActive?: boolean }
const path = () => '/environmental-variables'
export const environmentalVariableApi = {
  list: async (filters: EnvironmentalVariableFilters, signal: AbortSignal) => (await http.get<Page<EnvironmentalVariable>>(path(), { signal, params: filters })).data,
  get: async (id: string, signal?: AbortSignal) => (await http.get<EnvironmentalVariable>(`${path()}/${encodeURIComponent(id)}`, { signal })).data,
  create: async (values: EnvironmentalVariableValues) => (await http.post<EnvironmentalVariable>(path(), { ...values })).data,
  correct: async (id: string, values: EnvironmentalVariableValues, expectedVersion: number, reason: string) => (await http.put<EnvironmentalVariable>(`${path()}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  deactivate: async (id: string, expectedVersion: number, reason: string) => (await http.post<EnvironmentalVariable>(`${path()}/${encodeURIComponent(id)}/inactivation`, { expectedVersion, reason })).data,
  history: async (id: string, page: number, signal: AbortSignal) => (await http.get<Page<AuditEntry>>(`${path()}/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data,
}
