import { http } from '../../../services/http'
import { speciesApi } from '../../cultivation/services/SpeciesApi'

export interface SpeciesImage {
  base64: string
  mimeType: string
  sourceUrl: string
  title: string
  author: string
  license: string
  licenseUrl: string
  attribution: string
  changes: string
}
const images = new Map<string, SpeciesImage>()
export const speciesCatalogApi = {
  list: (page: number, signal: AbortSignal) => speciesApi.list({ page, pageSize: 9, isActive: true }, signal),
  async image(id: string, signal: AbortSignal) {
    const cached = images.get(id)
    if (cached) return cached
    const image = (await http.get<SpeciesImage>(`/species/${encodeURIComponent(id)}/image`, { signal })).data
    images.set(id, image)
    return image
  },
}
