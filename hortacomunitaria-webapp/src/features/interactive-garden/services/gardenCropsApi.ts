import { allPages } from '../../../services/pagination'
import type { GardenCrop } from '../cropVisuals'

export const gardenCropsApi = {
  list(gardenId: string, on: string, signal: AbortSignal) {
    return allPages<GardenCrop>(`/gardens/${encodeURIComponent(gardenId)}/crop-visuals`, signal, { on })
  },
}
