import { http } from "../../../services/http"
import type { Page } from "../../../types/api"
import type { AuditEntry } from "../../../types/records"
import type { AgronomicObservation, AgronomicObservationValues } from "../types/AgronomicObservation"
export interface AgronomicObservationFilters { from?: string; to?: string; page?: number; pageSize?: number; validity?: string; cycleId?: string; category?: string; bedId?: string; speciesId?: string }
const path = (garden: string) => `/gardens/${encodeURIComponent(garden)}/agronomic-observations`
export const agronomicObservationApi = {
  list: async (garden: string, filters: AgronomicObservationFilters, signal: AbortSignal) => (await http.get<Page<AgronomicObservation>>(path(garden), { signal, params: filters })).data,
  get: async (garden: string, id: string, signal?: AbortSignal) => (await http.get<AgronomicObservation>(`${path(garden)}/${encodeURIComponent(id)}`, { signal })).data,
  create: async (garden: string, values: AgronomicObservationValues, operationId: string) => (await http.post<AgronomicObservation>(path(garden), { operationId: operationId, cycleId: values.cycleId, observedOn: values.observedOn, category: values.category, description: values.description, measurement: values.measurement, unit: values.unit, method: values.method, responsibleParticipantId: values.responsibleParticipantId })).data,
  correct: async (garden: string, id: string, values: AgronomicObservationValues, expectedVersion: number, reason: string) => (await http.put<AgronomicObservation>(`${path(garden)}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  deactivate: async (garden: string, id: string, expectedVersion: number, reason: string) => (await http.post<AgronomicObservation>(`${path(garden)}/${encodeURIComponent(id)}/invalidation`, { expectedVersion, reason })).data,
  history: async (garden: string, id: string, page: number, signal: AbortSignal) => (await http.get<Page<AuditEntry>>(`${path(garden)}/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data,
}
