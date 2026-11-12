import { http } from '../../../services/http'
import type { Page } from '../../../types/api'
import type { UserProfile } from '../../auth/types'

/** Conta de acesso como retornada por /users. */
export interface User {
  id: string
  email: string
  displayName: string
  profile: UserProfile
  isActive: boolean
  mustChangePassword: boolean
  isLockedOut: boolean
  createdAt: string
  version: string
}

export interface CreateUserInput { email: string; displayName: string; profile: UserProfile; initialPassword: string }
export interface UpdateUserInput { displayName: string; profile: UserProfile; isActive: boolean; expectedVersion: string; reason: string }

export const userApi = {
  list: async (params: { page: number; pageSize: number; isActive?: boolean }, signal: AbortSignal) =>
    (await http.get<Page<User>>('/users', { params, signal })).data,
  get: async (id: string, signal: AbortSignal) => (await http.get<User>(`/users/${encodeURIComponent(id)}`, { signal })).data,
  create: async (input: CreateUserInput) => (await http.post<User>('/users', input)).data,
  update: async (id: string, input: UpdateUserInput) => (await http.put<User>(`/users/${encodeURIComponent(id)}`, input)).data,
}
