import { http } from "../../../services/http"
import type { Page } from "../../../types/api"
import type { AuditEntry } from "../../../types/records"
import type { EnvironmentalMeasurement, EnvironmentalMeasurementValues } from "../types/EnvironmentalMeasurement"
export interface EnvironmentalMeasurementFilters { from?: string; to?: string; page?: number; pageSize?: number; validity?: string; pointId?: string; variableId?: string; unit?: string; method?: string; quality?: string }
const path = (garden: string) => `/gardens/${encodeURIComponent(garden)}/environmental-measurements`
export const environmentalMeasurementApi = {
  list: async (garden: string, filters: EnvironmentalMeasurementFilters, signal: AbortSignal) => (await http.get<Page<EnvironmentalMeasurement>>(path(garden), { signal, params: filters })).data,
  get: async (garden: string, id: string, signal?: AbortSignal) => (await http.get<EnvironmentalMeasurement>(`${path(garden)}/${encodeURIComponent(id)}`, { signal })).data,
  create: async (garden: string, values: EnvironmentalMeasurementValues, operationId: string) => (await http.post<EnvironmentalMeasurement>(path(garden), { operationId: operationId, samplingPointId: values.samplingPointId, variableId: values.variableId, collectedAt: values.collectedAt, value: values.value, unit: values.unit, method: values.method, quality: values.quality, responsibleParticipantId: values.responsibleParticipantId, notes: values.notes })).data,
  correct: async (garden: string, id: string, values: EnvironmentalMeasurementValues, expectedVersion: number, reason: string) => (await http.put<EnvironmentalMeasurement>(`${path(garden)}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  deactivate: async (garden: string, id: string, expectedVersion: number, reason: string) => (await http.post<EnvironmentalMeasurement>(`${path(garden)}/${encodeURIComponent(id)}/invalidation`, { expectedVersion, reason })).data,
  history: async (garden: string, id: string, page: number, signal: AbortSignal) => (await http.get<Page<AuditEntry>>(`${path(garden)}/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data,
}
