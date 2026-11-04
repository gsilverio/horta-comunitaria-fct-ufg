import { http } from './http'
import type { Page } from '../types/api'

/** Transport helper: every page of a selector, with cancellation between pages. */
export async function allPages<T>(path: string, signal: AbortSignal, params: Record<string, string | boolean> = {}): Promise<T[]> {
  const items: T[] = []
  for (let page = 1; ; page++) {
    signal.throwIfAborted()
    const result = (await http.get<Page<T>>(path, { signal, params: { ...params, page, pageSize: 100 } })).data
    items.push(...result.items)
    if (!result.hasNextPage) return items
  }
}
