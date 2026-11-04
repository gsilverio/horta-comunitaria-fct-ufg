import { http } from "../../../services/http"
import type { Page } from "../../../types/api"
import type { AuditEntry } from "../../../types/records"
import type { Harvest, HarvestValues } from "../types/Harvest"
export interface HarvestFilters { from?: string; to?: string; page?: number; pageSize?: number; validity?: string; cycleId?: string; speciesId?: string }
const path = (garden: string) => `/gardens/${encodeURIComponent(garden)}/harvests`
export const harvestApi = {
  list: async (garden: string, filters: HarvestFilters, signal: AbortSignal) => (await http.get<Page<Harvest>>(path(garden), { signal, params: filters })).data,
  get: async (garden: string, id: string, signal?: AbortSignal) => (await http.get<Harvest>(`${path(garden)}/${encodeURIComponent(id)}`, { signal })).data,
  create: async (garden: string, values: HarvestValues, operationId: string) => (await http.post<Harvest>(path(garden), { operationId: operationId, cycleId: values.cycleId, harvestedOn: values.harvestedOn, freshMassKg: values.freshMassKg, usableMassKg: values.usableMassKg, lossReason: values.lossReason, responsibleParticipantId: values.responsibleParticipantId })).data,
  correct: async (garden: string, id: string, values: HarvestValues, expectedVersion: number, reason: string) => (await http.put<Harvest>(`${path(garden)}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  deactivate: async (garden: string, id: string, expectedVersion: number, reason: string) => (await http.post<Harvest>(`${path(garden)}/${encodeURIComponent(id)}/invalidation`, { expectedVersion, reason })).data,
  history: async (garden: string, id: string, page: number, signal: AbortSignal) => (await http.get<Page<AuditEntry>>(`${path(garden)}/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data,
}
