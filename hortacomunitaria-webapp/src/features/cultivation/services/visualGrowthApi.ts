import { http } from '../../../services/http'
import type { Species } from '../types/Species'
import type { Cultivar } from '../types/Cultivar'
import type { VisualGrowthValues } from '../types/VisualGrowth'

export const visualGrowthApi = {
  async species(id: string, values: VisualGrowthValues, expectedVersion: number, reason: string) {
    return (await http.put<Species>(`/species/${encodeURIComponent(id)}/visual-growth`, { ...values, expectedVersion, reason })).data
  },
  async cultivar(speciesId: string, id: string, values: VisualGrowthValues, expectedVersion: number, reason: string) {
    return (await http.put<Cultivar>(`/species/${encodeURIComponent(speciesId)}/cultivars/${encodeURIComponent(id)}/visual-growth`, { ...values, expectedVersion, reason })).data
  },
}
