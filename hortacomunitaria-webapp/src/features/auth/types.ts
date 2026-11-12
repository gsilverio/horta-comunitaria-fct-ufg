export type UserProfile = 'Administrator' | 'Operator'

export const profileLabels: Record<UserProfile, string> = { Administrator: 'Adm', Operator: 'Operador' }

/** Resposta de GET /auth/me. */
export interface CurrentUser {
  id: string
  email: string
  displayName: string
  profile: UserProfile
  mustChangePassword: boolean
}

export interface AuthContextValue {
  user: CurrentUser
  isAdministrator: boolean
  signOut: () => Promise<void>
  /** Conclui a troca de senha atualizando o usuário da sessão. */
  passwordChanged: () => Promise<void>
}
