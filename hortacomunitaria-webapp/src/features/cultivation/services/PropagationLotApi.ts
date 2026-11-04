import { http } from "../../../services/http"
import type { Page } from "../../../types/api"
import type { AuditEntry } from "../../../types/records"
import type { PropagationLot, PropagationLotValues } from "../types/PropagationLot"
export interface PropagationLotFilters { page?: number; pageSize?: number; isActive?: boolean }
const path = (garden: string) => `/gardens/${encodeURIComponent(garden)}/propagation-lots`
export const propagationLotApi = {
  list: async (garden: string, filters: PropagationLotFilters, signal: AbortSignal) => (await http.get<Page<PropagationLot>>(path(garden), { signal, params: filters })).data,
  get: async (garden: string, id: string, signal?: AbortSignal) => (await http.get<PropagationLot>(`${path(garden)}/${encodeURIComponent(id)}`, { signal })).data,
  create: async (garden: string, values: PropagationLotValues) => (await http.post<PropagationLot>(path(garden), { code: values.code, speciesId: values.speciesId, cultivarId: values.cultivarId, materialType: values.materialType, origin: values.origin, receivedOn: values.receivedOn, notes: values.notes })).data,
  correct: async (garden: string, id: string, values: PropagationLotValues, expectedVersion: number, reason: string) => (await http.put<PropagationLot>(`${path(garden)}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  deactivate: async (garden: string, id: string, expectedVersion: number, reason: string) => (await http.post<PropagationLot>(`${path(garden)}/${encodeURIComponent(id)}/inactivation`, { expectedVersion, reason })).data,
  history: async (garden: string, id: string, page: number, signal: AbortSignal) => (await http.get<Page<AuditEntry>>(`${path(garden)}/${encodeURIComponent(id)}/history`, { signal, params: { page, pageSize: 5 } })).data,
}
