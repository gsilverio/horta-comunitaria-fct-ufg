import { useRef, useState } from 'react'
import { apiFailure } from '../../../services/http'
import type { ApiFailure } from '../../../types/api'
import { gardenLayoutApi } from '../services/gardenLayoutApi'
import type { LayoutSnapshot, SaveLayoutAttempt } from '../services/gardenLayoutApi'
import type { DemoGarden } from '../types'

export function useLayoutSave(garden: string, initial?: LayoutSnapshot) {
  const [version, setVersion] = useState(initial?.version ?? 0)
  const [savedAt, setSavedAt] = useState(initial?.savedAt)
  const [busy, setBusy] = useState(false)
  const [uncertain, setUncertain] = useState(false)
  const [error, setError] = useState<ApiFailure>()
  const pending = useRef<SaveLayoutAttempt | undefined>(undefined)
  const sending = useRef(false)
  async function save(layout: DemoGarden) {
    if (sending.current || error?.status === 409) return
    sending.current = true; setBusy(true); setError(undefined)
    pending.current ??= { operationId: crypto.randomUUID(), expectedVersion: version, layout: { width: layout.width, height: layout.height, boundary: layout.boundary ?? null, elements: structuredClone(layout.elements) } }
    try {
      const saved = await gardenLayoutApi.save(garden, pending.current)
      setVersion(saved.version); setSavedAt(saved.savedAt); setUncertain(false); pending.current = undefined
      return saved
    } catch (failure) {
      const normalized = apiFailure(failure)
      setError(normalized)
      const unknown = normalized.status === undefined || normalized.status >= 500
      setUncertain(unknown)
      if (!unknown) pending.current = undefined
    } finally { sending.current = false; setBusy(false) }
  }
  return { save, version, savedAt, busy, uncertain, error }
}
