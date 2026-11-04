import { http } from "../../../services/http"
import type { Page } from "../../../types/api"
import type { AuditEntry } from "../../../types/records"
import type { Activity, ActivityValues } from "../types/Activity"
export interface ActivityFilters { from?: string; to?: string; page?: number; pageSize?: number; validity?: string; bedId?: string; sectorId?: string; cycleId?: string; occurrenceId?: string; category?: string; isSocial?: boolean; isEducational?: boolean; institutionId?: string }
const path = (garden: string) => `/gardens/${encodeURIComponent(garden)}/activities`
export const activityApi = {
  list: async (garden: string, filters: ActivityFilters, signal: AbortSignal) => (await http.get<Page<Activity>>(path(garden), { signal, params: filters })).data,
  get: async (garden: string, id: string, signal?: AbortSignal) => (await http.get<Activity>(`${path(garden)}/${encodeURIComponent(id)}`, { signal })).data,
  create: async (garden: string, values: ActivityValues, operationId: string) => (await http.post<Activity>(path(garden), { operationId: operationId, title: values.title, category: values.category, performedOn: values.performedOn, description: values.description, bedId: values.bedId, sectorId: values.sectorId, cycleId: values.cycleId, occurrenceId: values.occurrenceId, isOperational: values.isOperational, isSocial: values.isSocial, isEducational: values.isEducational, objective: values.objective, modality: values.modality, reportedAudienceTotal: values.reportedAudienceTotal, audienceListComplete: values.audienceListComplete, participants: values.participants, inputUsages: values.inputUsages, institutions: values.institutions })).data,
  correct: async (garden: string, id: string, values: ActivityValues, expectedVersion: number, reason: string) => (await http.put<Activity>(`${path(garden)}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  deactivate: async (garden: string, id: string, expectedVersion: number, reason: string) => (await http.post<Activity>(`${path(garden)}/${encodeURIComponent(id)}/invalidation`, { expectedVersion, reason })).data,
  history: async (garden: string, id: string, page: number, signal: AbortSignal) => (await http.get<Page<AuditEntry>>(`${path(garden)}/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data,
}
