import axios from 'axios'
import type { ApiFailure } from '../types/api'
import { expireSession, getAccessToken, refreshSession } from './session'

export const http = axios.create({ baseURL: import.meta.env.VITE_API_BASE_URL || '/api/v1', timeout: 15000, withCredentials: true })

declare module 'axios' {
  interface AxiosRequestConfig {
    /** Login e troca de senha tratam 401 como resposta, sem tentar renovar a sessão. */
    skipAuthRefresh?: boolean
    /** Marca a única nova tentativa após renovar a sessão. */
    authRetried?: boolean
  }
}

http.interceptors.request.use(config => {
  const token = getAccessToken()
  if (token) config.headers.set('Authorization', `Bearer ${token}`)
  return config
})
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
      : status === 401 ? 'Sessão expirada. Entre novamente.'
      : status === 403 ? 'Seu perfil não permite esta ação.'
      : status === 429 ? 'Muitas tentativas. Aguarde um minuto e tente novamente.'
      : status && status >= 500 ? 'O serviço está indisponível. Tente novamente.'
      : error.code === 'ECONNABORTED' ? 'O serviço demorou para responder. Verifique a conexão e tente novamente.'
      : 'Não foi possível acessar a API. Verifique se o serviço está em execução.'
    const trace = error.response?.headers['x-trace-id']
    // Respostas de autenticação/autorização trazem a mensagem pública no título do ProblemDetails.
    const title = status && [401, 403, 429].includes(status) && object(data) && typeof data.title === 'string' ? data.title : undefined
    return { message: object(data) && typeof data.detail === 'string' ? data.detail : title ?? fallback,
      status, code: object(data) && typeof data.code === 'string' ? data.code : undefined,
      traceId: object(data) && typeof data.traceId === 'string' ? data.traceId : typeof trace === 'string' ? trace : undefined, fields }
  }
  return { message: error instanceof Error ? error.message : 'Não foi possível concluir a operação.', fields: {} }
}

http.interceptors.response.use(response => response, async (error: unknown) => {
  const config = axios.isAxiosError(error) ? error.config : undefined
  if (axios.isAxiosError(error) && error.response?.status === 401 && config && !config.skipAuthRefresh && !config.authRetried) {
    // Access token expirado: uma única renovação compartilhada e nova tentativa da mesma requisição.
    if (await refreshSession()) return http.request({ ...config, authRetried: true })
    expireSession()
  }
  if (!axios.isCancel(error)) {
    const failure = apiFailure(error)
    console.warn('ApiRequestFailed', { status: failure.status, code: failure.code, traceId: failure.traceId })
  }
  return Promise.reject(error)
})
