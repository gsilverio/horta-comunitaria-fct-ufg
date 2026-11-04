import { http } from "../../../services/http"
import type { Page } from "../../../types/api"
import type { AuditEntry } from "../../../types/records"
import type { FinancialEntry, FinancialEntryValues } from "../types/FinancialEntry"
export interface FinancialEntryFilters { from?: string; to?: string; page?: number; pageSize?: number; validity?: string; category?: string; nature?: string; bedId?: string; cycleId?: string }
const path = (garden: string) => `/gardens/${encodeURIComponent(garden)}/financial-entries`
export const financialEntryApi = {
  list: async (garden: string, filters: FinancialEntryFilters, signal: AbortSignal) => (await http.get<Page<FinancialEntry>>(path(garden), { signal, params: filters })).data,
  get: async (garden: string, id: string, signal?: AbortSignal) => (await http.get<FinancialEntry>(`${path(garden)}/${encodeURIComponent(id)}`, { signal })).data,
  create: async (garden: string, values: FinancialEntryValues, operationId: string) => (await http.post<FinancialEntry>(path(garden), { operationId: operationId, entryDate: values.entryDate, description: values.description, category: values.category, amountBrl: values.amountBrl, nature: values.nature, bedId: values.bedId, cycleId: values.cycleId })).data,
  correct: async (garden: string, id: string, values: FinancialEntryValues, expectedVersion: number, reason: string) => (await http.put<FinancialEntry>(`${path(garden)}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  deactivate: async (garden: string, id: string, expectedVersion: number, reason: string) => (await http.post<FinancialEntry>(`${path(garden)}/${encodeURIComponent(id)}/invalidation`, { expectedVersion, reason })).data,
  history: async (garden: string, id: string, page: number, signal: AbortSignal) => (await http.get<Page<AuditEntry>>(`${path(garden)}/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data,
}
