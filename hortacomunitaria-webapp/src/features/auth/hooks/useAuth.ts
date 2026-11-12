import { createContext, useContext } from 'react'
import type { AuthContextValue } from '../types'

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)

/** Sessão autenticada; disponível em toda a aplicação abaixo do provedor de autenticação. */
export function useAuth() {
  const value = useContext(AuthContext)
  if (!value) throw new Error('useAuth exige o provedor de autenticação.')
  return value
}

/**
 * Ações de estrutura e catálogos (criar, editar, inativar) pertencem ao perfil Adm. A interface só oculta;
 * o servidor continua recusando a ação para o Operador.
 */
export const ManageAccess = createContext(true)
export const useCanManage = () => useContext(ManageAccess)
