import { http } from "../../../services/http"
import type { Page } from "../../../types/api"
import type { AuditEntry } from "../../../types/records"
import type { IrrigationSector, IrrigationSectorValues } from "../types/IrrigationSector"
export interface IrrigationSectorFilters { page?: number; pageSize?: number; isActive?: boolean }
const path = (garden: string) => `/gardens/${encodeURIComponent(garden)}/sectors`
export const irrigationSectorApi = {
  list: async (garden: string, filters: IrrigationSectorFilters, signal: AbortSignal) => (await http.get<Page<IrrigationSector>>(`${path(garden)}/catalog`, { signal, params: filters })).data,
  get: async (garden: string, id: string, signal?: AbortSignal) => (await http.get<IrrigationSector>(`${path(garden)}/${encodeURIComponent(id)}`, { signal })).data,
  create: async (garden: string, values: IrrigationSectorValues) => (await http.post<IrrigationSector>(path(garden), { code: values.code, name: values.name, description: values.description })).data,
  correct: async (garden: string, id: string, values: IrrigationSectorValues, expectedVersion: number, reason: string) => (await http.put<IrrigationSector>(`${path(garden)}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  deactivate: async (garden: string, id: string, expectedVersion: number, reason: string) => (await http.post<IrrigationSector>(`${path(garden)}/${encodeURIComponent(id)}/inactivation`, { expectedVersion, reason })).data,
  history: async (garden: string, id: string, page: number, signal: AbortSignal) => (await http.get<Page<AuditEntry>>(`${path(garden)}/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data,
}
