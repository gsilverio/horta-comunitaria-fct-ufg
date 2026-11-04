import { http } from "../../../services/http"
import type { Page } from "../../../types/api"
import type { AuditEntry } from "../../../types/records"
import type { Planting, PlantingValues } from "../types/Planting"
export interface PlantingFilters { from?: string; to?: string; page?: number; pageSize?: number; validity?: string; cycleId?: string; kind?: string; bedId?: string; speciesId?: string }
const path = (garden: string) => `/gardens/${encodeURIComponent(garden)}/plantings`
export const plantingApi = {
  list: async (garden: string, filters: PlantingFilters, signal: AbortSignal) => (await http.get<Page<Planting>>(path(garden), { signal, params: filters })).data,
  get: async (garden: string, id: string, signal?: AbortSignal) => (await http.get<Planting>(`${path(garden)}/${encodeURIComponent(id)}`, { signal })).data,
  create: async (garden: string, values: PlantingValues, operationId: string) => (await http.post<Planting>(path(garden), { operationId: operationId, cycleId: values.cycleId, propagationLotId: values.propagationLotId, replacedPlantingId: values.replacedPlantingId, plantedOn: values.plantedOn, kind: values.kind, plantedQuantity: values.plantedQuantity, plantSpacingCm: values.plantSpacingCm, rowSpacingCm: values.rowSpacingCm, responsibleParticipantId: values.responsibleParticipantId })).data,
  correct: async (garden: string, id: string, values: PlantingValues, expectedVersion: number, reason: string) => (await http.put<Planting>(`${path(garden)}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  deactivate: async (garden: string, id: string, expectedVersion: number, reason: string) => (await http.post<Planting>(`${path(garden)}/${encodeURIComponent(id)}/invalidation`, { expectedVersion, reason })).data,
  history: async (garden: string, id: string, page: number, signal: AbortSignal) => (await http.get<Page<AuditEntry>>(`${path(garden)}/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data,
}
