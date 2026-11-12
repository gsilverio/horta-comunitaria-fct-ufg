import { http } from '../../../services/http'
import { refreshSession, setAccessToken } from '../../../services/session'
import type { CurrentUser } from '../types'

interface TokenResponse { accessToken: string; tokenType: 'Bearer'; expiresIn: number }

export const authApi = {
  async login(email: string, password: string) {
    const response = await http.post<TokenResponse>('/auth/login', { email, password }, { skipAuthRefresh: true })
    setAccessToken(response.data.accessToken)
  },
  async changePassword(currentPassword: string, newPassword: string) {
    const response = await http.post<TokenResponse>('/auth/change-password', { currentPassword, newPassword }, { skipAuthRefresh: true })
    setAccessToken(response.data.accessToken)
  },
  async logout() {
    try { await http.post('/auth/logout', undefined, { skipAuthRefresh: true }) } finally { setAccessToken(undefined) }
  },
  me: async (signal?: AbortSignal) => (await http.get<CurrentUser>('/auth/me', { signal })).data,
  /** Recupera a sessão após recarregar a página, pelo cookie HttpOnly de renovação. */
  restore: refreshSession,
}
