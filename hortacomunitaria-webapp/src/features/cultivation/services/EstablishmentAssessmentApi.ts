import { http } from "../../../services/http"
import type { Page } from "../../../types/api"
import type { AuditEntry } from "../../../types/records"
import type { EstablishmentAssessment, EstablishmentAssessmentValues } from "../types/EstablishmentAssessment"
export interface EstablishmentAssessmentFilters { from?: string; to?: string; page?: number; pageSize?: number; validity?: string; plantingId?: string; method?: string; cycleId?: string; speciesId?: string }
const path = (garden: string) => `/gardens/${encodeURIComponent(garden)}/establishment-assessments`
export const establishmentAssessmentApi = {
  list: async (garden: string, filters: EstablishmentAssessmentFilters, signal: AbortSignal) => (await http.get<Page<EstablishmentAssessment>>(path(garden), { signal, params: filters })).data,
  get: async (garden: string, id: string, signal?: AbortSignal) => (await http.get<EstablishmentAssessment>(`${path(garden)}/${encodeURIComponent(id)}`, { signal })).data,
  create: async (garden: string, values: EstablishmentAssessmentValues, operationId: string) => (await http.post<EstablishmentAssessment>(path(garden), { operationId: operationId, plantingId: values.plantingId, assessedOn: values.assessedOn, survivingQuantity: values.survivingQuantity, method: values.method, notes: values.notes, responsibleParticipantId: values.responsibleParticipantId })).data,
  correct: async (garden: string, id: string, values: EstablishmentAssessmentValues, expectedVersion: number, reason: string) => (await http.put<EstablishmentAssessment>(`${path(garden)}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  deactivate: async (garden: string, id: string, expectedVersion: number, reason: string) => (await http.post<EstablishmentAssessment>(`${path(garden)}/${encodeURIComponent(id)}/invalidation`, { expectedVersion, reason })).data,
  history: async (garden: string, id: string, page: number, signal: AbortSignal) => (await http.get<Page<AuditEntry>>(`${path(garden)}/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data,
}
