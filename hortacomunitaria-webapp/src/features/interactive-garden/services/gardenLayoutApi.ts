import { http } from '../../../services/http'
import type { DemoGarden } from '../types'

export interface LayoutSnapshot {
  version: number
  savedAt: string | null
  layout: DemoGarden | null
}
export interface SaveLayoutAttempt {
  operationId: string
  expectedVersion: number
  layout: { width: number; height: number; boundary: DemoGarden['boundary'] | null; elements: DemoGarden['elements'] }
}
const path = (garden: string) => `/gardens/${encodeURIComponent(garden)}/layout`
export const gardenLayoutApi = {
  async get(garden: string, signal?: AbortSignal): Promise<LayoutSnapshot> {
    return (await http.get<LayoutSnapshot>(path(garden), { signal })).data
  },
  async save(garden: string, attempt: SaveLayoutAttempt): Promise<LayoutSnapshot> {
    return (await http.put<LayoutSnapshot>(path(garden), attempt)).data
  },
}
