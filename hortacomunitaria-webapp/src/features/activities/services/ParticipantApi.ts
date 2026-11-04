import { http } from "../../../services/http"
import type { Page } from "../../../types/api"
import type { AuditEntry } from "../../../types/records"
import type { Participant, ParticipantValues } from "../types/Participant"
export interface ParticipantFilters { page?: number; pageSize?: number; isActive?: boolean }
const path = (garden: string) => `/gardens/${encodeURIComponent(garden)}/participants`
export const participantApi = {
  list: async (garden: string, filters: ParticipantFilters, signal: AbortSignal) => (await http.get<Page<Participant>>(`${path(garden)}/catalog`, { signal, params: filters })).data,
  get: async (garden: string, id: string, signal?: AbortSignal) => (await http.get<Participant>(`${path(garden)}/${encodeURIComponent(id)}`, { signal })).data,
  create: async (garden: string, values: ParticipantValues) => (await http.post<Participant>(path(garden), { displayName: values.displayName, category: values.category })).data,
  correct: async (garden: string, id: string, values: ParticipantValues, expectedVersion: number, reason: string) => (await http.put<Participant>(`${path(garden)}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  deactivate: async (garden: string, id: string, expectedVersion: number, reason: string) => (await http.post<Participant>(`${path(garden)}/${encodeURIComponent(id)}/inactivation`, { expectedVersion, reason })).data,
  history: async (garden: string, id: string, page: number, signal: AbortSignal) => (await http.get<Page<AuditEntry>>(`${path(garden)}/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data,
}
