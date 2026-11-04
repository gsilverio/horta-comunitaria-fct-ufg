import { useCallback, useState } from 'react'
import { useRemote } from './useRemote'
import { lookupApi } from '../services/lookups'
import type { LookupKey } from '../services/lookups'
export function useLookups(garden: string, keys: readonly LookupKey[]) {
  const [revision, setRevision] = useState(0), key = keys.join('|')
  const load = useCallback((signal: AbortSignal) => lookupApi.load(garden, key, signal), [garden, key])
  return { ...useRemote(`${garden}/${key}/${revision}`, load), retry: () => setRevision(x => x + 1) }
}
