import axios from 'axios'
import type { ApiFailure } from '../types/api'

export const http = axios.create({ baseURL: import.meta.env.VITE_API_BASE_URL || '/api/v1', timeout: 15000 })
const object = (value: unknown): value is Record<string, unknown> => typeof value === 'object' && value !== null

export function apiFailure(error: unknown): ApiFailure {
  if (axios.isAxiosError<unknown>(error)) {
    const data = error.response?.data
    const fields: Record<string, string[]> = {}
    if (object(data) && object(data.errors)) {
      for (const [key, messages] of Object.entries(data.errors)) {
        if (Array.isArray(messages) && messages.every((item): item is string => typeof item === 'string')) {
          fields[key.replace(/(^|\.)[A-Z]/g, part => part.toLowerCase())] = messages
        }
      }
    }
    const status = error.response?.status
    const fallback = status === 400 ? 'Revise os dados enviados e os campos indicados.'
      : status === 409 ? 'Este registro mudou. Recarregue e revise antes de salvar.'
      : status === 404 ? 'Registro ou referência não encontrado nesta horta.'
      : status && status >= 500 ? 'O serviço está indisponível. Tente novamente.'
      : error.code === 'ECONNABORTED' ? 'O serviço demorou para responder. Verifique a conexão e tente novamente.'
      : 'Não foi possível acessar a API. Verifique se o serviço está em execução.'
    const trace = error.response?.headers['x-trace-id']
    return { message: object(data) && typeof data.detail === 'string' ? data.detail : fallback,
      status, code: object(data) && typeof data.code === 'string' ? data.code : undefined,
      traceId: object(data) && typeof data.traceId === 'string' ? data.traceId : typeof trace === 'string' ? trace : undefined, fields }
  }
  return { message: error instanceof Error ? error.message : 'Não foi possível concluir a operação.', fields: {} }
}

http.interceptors.response.use(response => response, (error: unknown) => {
  if (!axios.isCancel(error)) {
    const failure = apiFailure(error)
    console.warn('ApiRequestFailed', { status: failure.status, code: failure.code, traceId: failure.traceId })
  }
  return Promise.reject(error)
})
