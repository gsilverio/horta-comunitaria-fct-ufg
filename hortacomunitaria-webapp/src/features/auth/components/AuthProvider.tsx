import { useCallback, useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { onSessionExpired } from '../../../services/session'
import { AuthContext } from '../hooks/useAuth'
import { LoginPage } from '../pages/LoginPage'
import { AuthShell } from './AuthShell'
import { ChangePasswordForm } from './ChangePasswordForm'
import { authApi } from '../services/authApi'
import type { CurrentUser } from '../types'

type State = { status: 'loading' } | { status: 'anonymous'; expired: boolean } | { status: 'authenticated'; user: CurrentUser }

/**
 * Portão de autenticação: recupera a sessão pelo cookie ao abrir, mostra a entrada quando não há sessão e a troca
 * obrigatória enquanto a senha inicial não foi substituída. O endereço atual é preservado durante a entrada.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<State>({ status: 'loading' })

  const loadUser = useCallback(async () => {
    const user = await authApi.me()
    setState({ status: 'authenticated', user })
  }, [])

  useEffect(() => {
    let active = true
    authApi.restore()
      .then(restored => restored ? authApi.me() : undefined)
      .then(user => { if (active) setState(user ? { status: 'authenticated', user } : { status: 'anonymous', expired: false }) },
        () => { if (active) setState({ status: 'anonymous', expired: false }) })
    const stop = onSessionExpired(() => setState(current => ({ status: 'anonymous', expired: current.status === 'authenticated' })))
    return () => { active = false; stop() }
  }, [])

  const signOut = useCallback(async () => {
    await authApi.logout().catch(() => undefined)
    setState({ status: 'anonymous', expired: false })
  }, [])

  if (state.status === 'loading') return <p role="status" className="plant-status p-6">Carregando sessão…</p>
  if (state.status === 'anonymous') return <LoginPage signedIn={loadUser} expired={state.expired} />
  if (state.user.mustChangePassword) return <AuthShell title="Defina sua senha" description="Por segurança, troque a senha inicial antes de continuar.">
    <ChangePasswordForm done={loadUser} />
    <button type="button" className="text-button mt-4" onClick={() => void signOut()}>Sair</button>
  </AuthShell>
  return <AuthContext.Provider value={{ user: state.user, isAdministrator: state.user.profile === 'Administrator', signOut, passwordChanged: loadUser }}>
    {children}
  </AuthContext.Provider>
}
