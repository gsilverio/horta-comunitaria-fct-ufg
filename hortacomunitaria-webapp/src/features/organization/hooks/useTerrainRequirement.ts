import { useCallback, useState } from 'react'
import { useRemote } from '../../../hooks/useRemote'
import { gardenLayoutApi } from '../../interactive-garden/services/gardenLayoutApi'
import { irrigationSectorApi } from '../services/IrrigationSectorApi'

export interface TerrainState {
  /** Contorno do terreno salvo no mapa da horta. */
  hasTerrain: boolean
  /** Setores já cadastrados (inclusive anteriores ao mapa). */
  hasSectors: boolean
}

/** Horta nova: sem terreno salvo e sem setores. Setores e canteiros só fazem sentido depois do terreno. */
export const terrainBlocks = (state?: TerrainState) => Boolean(state && !state.hasTerrain && !state.hasSectors)

/**
 * Confere se a horta já tem terreno salvo no mapa. `refreshKey` força nova consulta (ex.: ao trocar de página).
 * Com `garden` vazio não consulta a API.
 */
export function useTerrainRequirement(garden: string, refreshKey = '') {
  const [revision, setRevision] = useState(0)
  const load = useCallback(async (signal: AbortSignal): Promise<TerrainState | undefined> => {
    if (!garden) return undefined
    const [layout, sectors] = await Promise.all([
      gardenLayoutApi.get(garden, signal),
      irrigationSectorApi.list(garden, { page: 1, pageSize: 1 }, signal),
    ])
    return { hasTerrain: Boolean(layout.layout?.boundary && layout.layout.boundary.length >= 3), hasSectors: sectors.totalCount > 0 }
  }, [garden])
  const remote = useRemote(`terrain/${garden}/${refreshKey}/${revision}`, load)
  return { ...remote, retry: () => setRevision(value => value + 1) }
}
