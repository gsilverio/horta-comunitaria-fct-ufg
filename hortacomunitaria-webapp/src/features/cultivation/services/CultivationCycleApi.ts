import { http } from "../../../services/http"
import type { Page } from "../../../types/api"
import type { AuditEntry } from "../../../types/records"
import type { CultivationCycle, CultivationCycleValues } from "../types/CultivationCycle"
export interface CultivationCycleFilters { from?: string; to?: string; page?: number; pageSize?: number; validity?: string; bedId?: string; speciesId?: string; status?: string }
const path = (garden: string) => `/gardens/${encodeURIComponent(garden)}/cultivation-cycles`
export const cultivationCycleApi = {
  list: async (garden: string, filters: CultivationCycleFilters, signal: AbortSignal) => (await http.get<Page<CultivationCycle>>(path(garden), { signal, params: filters })).data,
  get: async (garden: string, id: string, signal?: AbortSignal) => (await http.get<CultivationCycle>(`${path(garden)}/${encodeURIComponent(id)}`, { signal })).data,
  create: async (garden: string, values: CultivationCycleValues, operationId: string) => (await http.post<CultivationCycle>(path(garden), { operationId: operationId, bedId: values.bedId, speciesId: values.speciesId, cultivarId: values.cultivarId, startedOn: values.startedOn, occupiedAreaM2: values.occupiedAreaM2, responsibleParticipantId: values.responsibleParticipantId })).data,
  correct: async (garden: string, id: string, values: CultivationCycleValues, expectedVersion: number, reason: string) => (await http.put<CultivationCycle>(`${path(garden)}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  deactivate: async (garden: string, id: string, expectedVersion: number, reason: string) => (await http.post<CultivationCycle>(`${path(garden)}/${encodeURIComponent(id)}/invalidation`, { expectedVersion, reason })).data,
  history: async (garden: string, id: string, page: number, signal: AbortSignal) => (await http.get<Page<AuditEntry>>(`${path(garden)}/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data,
  close: async (garden: string, id: string, endedOn: string, expectedVersion: number, reason: string) => (await http.post<CultivationCycle>(`${path(garden)}/${encodeURIComponent(id)}/closure`, { endedOn, expectedVersion, reason })).data,
}
