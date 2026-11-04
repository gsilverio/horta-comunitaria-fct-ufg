import { http } from "../../../services/http"
import type { Page } from "../../../types/api"
import type { AuditEntry } from "../../../types/records"
import type { Occurrence, OccurrenceValues } from "../types/Occurrence"
export interface OccurrenceFilters { from?: string; to?: string; page?: number; pageSize?: number; validity?: string; category?: string; status?: string; bedId?: string; sectorId?: string; cycleId?: string; irrigationId?: string }
const path = (garden: string) => `/gardens/${encodeURIComponent(garden)}/occurrences`
export const occurrenceApi = {
  list: async (garden: string, filters: OccurrenceFilters, signal: AbortSignal) => (await http.get<Page<Occurrence>>(path(garden), { signal, params: filters })).data,
  get: async (garden: string, id: string, signal?: AbortSignal) => (await http.get<Occurrence>(`${path(garden)}/${encodeURIComponent(id)}`, { signal })).data,
  create: async (garden: string, values: OccurrenceValues, operationId: string) => (await http.post<Occurrence>(path(garden), { operationId: operationId, openedAt: values.openedAt, category: values.category, description: values.description, responsibleParticipantId: values.responsibleParticipantId, bedId: values.bedId, sectorId: values.sectorId, cycleId: values.cycleId, irrigationId: values.irrigationId })).data,
  correct: async (garden: string, id: string, values: OccurrenceValues, expectedVersion: number, reason: string) => (await http.put<Occurrence>(`${path(garden)}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  deactivate: async (garden: string, id: string, expectedVersion: number, reason: string) => (await http.post<Occurrence>(`${path(garden)}/${encodeURIComponent(id)}/invalidation`, { expectedVersion, reason })).data,
  history: async (garden: string, id: string, page: number, signal: AbortSignal) => (await http.get<Page<AuditEntry>>(`${path(garden)}/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data,
  status: async (garden: string, id: string, status: 'Open' | 'InProgress', expectedVersion: number, reason: string) => (await http.post<Occurrence>(`${path(garden)}/${encodeURIComponent(id)}/status`, { status, expectedVersion, reason })).data,
  resolve: async (garden: string, id: string, resolvedAt: string, resolution: string, expectedVersion: number, reason: string) => (await http.post<Occurrence>(`${path(garden)}/${encodeURIComponent(id)}/resolution`, { resolvedAt, resolution, expectedVersion, reason })).data,
}
