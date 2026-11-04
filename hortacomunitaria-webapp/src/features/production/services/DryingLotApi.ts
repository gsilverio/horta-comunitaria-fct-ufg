import { http } from "../../../services/http"
import type { Page } from "../../../types/api"
import type { AuditEntry } from "../../../types/records"
import type { DryingLot, DryingLotValues } from "../types/DryingLot"
export interface DryingLotFilters { from?: string; to?: string; page?: number; pageSize?: number; validity?: string; status?: string }
const path = (garden: string) => `/gardens/${encodeURIComponent(garden)}/drying-lots`
export const dryingLotApi = {
  list: async (garden: string, filters: DryingLotFilters, signal: AbortSignal) => (await http.get<Page<DryingLot>>(path(garden), { signal, params: filters })).data,
  get: async (garden: string, id: string, signal?: AbortSignal) => (await http.get<DryingLot>(`${path(garden)}/${encodeURIComponent(id)}`, { signal })).data,
  create: async (garden: string, values: DryingLotValues, operationId: string) => (await http.post<DryingLot>(path(garden), { operationId: operationId, code: values.code, method: values.method, startedOn: values.startedOn, responsibleParticipantId: values.responsibleParticipantId, items: values.items })).data,
  correct: async (garden: string, id: string, values: DryingLotValues, expectedVersion: number, reason: string) => (await http.put<DryingLot>(`${path(garden)}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  deactivate: async (garden: string, id: string, expectedVersion: number, reason: string) => (await http.post<DryingLot>(`${path(garden)}/${encodeURIComponent(id)}/invalidation`, { expectedVersion, reason })).data,
  history: async (garden: string, id: string, page: number, signal: AbortSignal) => (await http.get<Page<AuditEntry>>(`${path(garden)}/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data,
  complete: async (garden: string, id: string, endedOn: string, totalDryMassKg: number, expectedVersion: number, reason: string) => (await http.post<DryingLot>(`${path(garden)}/${encodeURIComponent(id)}/completion`, { endedOn, totalDryMassKg, expectedVersion, reason })).data,
}
