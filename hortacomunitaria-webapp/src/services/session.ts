import axios from 'axios'

/**
 * Access token apenas em memória (nunca em localStorage/sessionStorage). Ao recarregar a página, a sessão é
 * recuperada pelo refresh token, que o navegador guarda em cookie HttpOnly inacessível ao JavaScript.
 */
let accessToken: string | undefined
let pendingRefresh: Promise<boolean> | undefined
const expiredListeners = new Set<() => void>()

/** Cliente sem interceptores, usado só para renovar: evita recursão quando a própria renovação responde 401. */
const refreshClient = axios.create({ baseURL: import.meta.env.VITE_API_BASE_URL || '/api/v1', timeout: 15000, withCredentials: true })

export const getAccessToken = () => accessToken
export function setAccessToken(token: string | undefined) { accessToken = token }

/** Renova o access token pelo cookie. Chamadas simultâneas compartilham a mesma renovação. */
export function refreshSession(): Promise<boolean> {
  pendingRefresh ??= refreshClient.post<{ accessToken: string }>('/auth/refresh')
    .then(response => { accessToken = response.data.accessToken; return true })
    .catch(() => { accessToken = undefined; return false })
    .finally(() => { pendingRefresh = undefined })
  return pendingRefresh
}

/** Sessão encerrada pelo servidor (renovação recusada): avisa o provedor de autenticação. */
export function expireSession() {
  accessToken = undefined
  expiredListeners.forEach(listener => listener())
}

export function onSessionExpired(listener: () => void) {
  expiredListeners.add(listener)
  return () => { expiredListeners.delete(listener) }
}
