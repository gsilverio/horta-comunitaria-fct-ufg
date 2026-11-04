export interface Page<T> {
  items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number
  hasPreviousPage: boolean; hasNextPage: boolean
}
export interface ApiFailure {
  message: string; status?: number; code?: string; traceId?: string
  fields: Record<string, string[]>
}
