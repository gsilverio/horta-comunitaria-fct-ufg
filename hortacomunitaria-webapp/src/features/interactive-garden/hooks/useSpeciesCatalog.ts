import { useCallback, useState } from 'react'
import { useRemote } from '../../../hooks/useRemote'
import { speciesCatalogApi } from '../services/speciesCatalogApi'

export function useSpeciesCatalog() {
  const [page, setPage] = useState(1), [revision, setRevision] = useState(0)
  const load = useCallback((signal: AbortSignal) => speciesCatalogApi.list(page, signal), [page])
  return { ...useRemote(`species/${page}/${revision}`, load), page, changePage: setPage, retry: () => setRevision(value => value + 1) }
}

export function useSpeciesImage(id: string, hasImage: boolean) {
  const [revision, setRevision] = useState(0)
  const load = useCallback((signal: AbortSignal) => hasImage ? speciesCatalogApi.image(id, signal) : Promise.resolve(undefined), [id, hasImage])
  return { ...useRemote(`species-image/${id}/${revision}`, load), retry: () => setRevision(value => value + 1) }
}
