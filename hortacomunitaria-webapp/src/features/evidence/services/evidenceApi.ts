import { http } from '../../../services/http'
import type { Page } from '../../../types/api'
import type { Evidence, EvidenceTarget, EvidenceUpload } from '../types/Evidence'

export interface EvidenceFilters { targetType?: EvidenceTarget; targetId?: string; validity?: 'Valid' | 'Invalid'; page?: number; pageSize?: number }
const path = (garden: string) => `/gardens/${encodeURIComponent(garden)}/evidences`

export const evidenceApi = {
  list: async (garden: string, filters: EvidenceFilters, signal: AbortSignal) => (await http.get<Page<Evidence>>(path(garden), { signal, params: filters })).data,
  /** Envio multipart; a mesma operationId deve ser reutilizada ao repetir uma tentativa incerta. */
  upload: async (garden: string, values: EvidenceUpload, operationId: string, progress?: (fraction: number) => void) => {
    const form = new FormData()
    form.append('file', values.file, values.fileName)
    form.append('operationId', operationId)
    form.append('targetType', values.targetType)
    form.append('targetId', values.targetId)
    form.append('title', values.title)
    if (values.description) form.append('description', values.description)
    if (values.capturedOn) form.append('capturedOn', values.capturedOn)
    return (await http.post<Evidence>(path(garden), form, { timeout: 120000, onUploadProgress: event => { if (progress && event.total) progress(event.loaded / event.total) } })).data
  },
  correct: async (garden: string, id: string, values: { title: string; description: string | null; capturedOn: string | null }, expectedVersion: number, reason: string) =>
    (await http.put<Evidence>(`${path(garden)}/${encodeURIComponent(id)}`, { ...values, expectedVersion, reason })).data,
  invalidate: async (garden: string, id: string, expectedVersion: number, reason: string) =>
    (await http.post<Evidence>(`${path(garden)}/${encodeURIComponent(id)}/invalidation`, { expectedVersion, reason })).data,
  /** URL da imagem para <img>; usa a mesma base do cliente HTTP (proxy do Vite em desenvolvimento). */
  contentUrl: (garden: string, id: string) => `${http.defaults.baseURL ?? ''}${path(garden)}/${encodeURIComponent(id)}/content`,
}
