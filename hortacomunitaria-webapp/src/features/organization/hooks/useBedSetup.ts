import { useRef, useState } from 'react'
import { FormFailure } from '../../irrigation/formValues'
import { apiFailure } from '../../../services/http'
import { releaseNavigationGuard } from '../../../services/navigationGuard'
import type { ApiFailure } from '../../../types/api'
import type { Bed } from '../types/Bed'
import { bedContextApi } from '../services/bedContextApi'

export interface BedSetupValues { area: number; sectorId: string; validFrom: string }
export function useBedSetup(garden: string, bed: Bed, saved: (record: Bed) => void) {
  const [busy, setBusy] = useState(false), [error, setError] = useState<ApiFailure>(), [pending, setPending] = useState(false)
  const [conflict, setConflict] = useState(false)
  const attempt = useRef<BedSetupValues | undefined>(undefined), sending = useRef(false)
  async function submit(values: () => BedSetupValues) {
    if (sending.current || conflict) return
    sending.current = true; setBusy(true); setError(undefined)
    let writing = false
    try {
      const payload = attempt.current ?? values()
      attempt.current = payload
      const current = await bedContextApi.on(garden, bed.id, payload.validFrom, new AbortController().signal)
      if (!current.isActive) throw new Error('Este canteiro foi inativado. Volte ao detalhe para conferir.')
      if (current.configuration && current.configuration.cultivableAreaM2 !== payload.area || current.sectorAssignment && current.sectorAssignment.sectorId !== payload.sectorId) {
        setConflict(true)
        throw new Error('A área ou o setor mudou enquanto você preenchia. Volte ao canteiro para revisar os dados confirmados.')
      }
      let version = current.version
      if (!current.configuration) {
        writing = true; setPending(true)
        const configured = await bedContextApi.configure(garden, bed.id, { cultivableAreaM2: payload.area, lengthMeters: null, widthMeters: null, validFrom: payload.validFrom, expectedBedVersion: version, reason: null })
        version = configured.bedVersion
      }
      if (!current.sectorAssignment) {
        writing = true; setPending(true)
        await bedContextApi.assign(garden, bed.id, { sectorId: payload.sectorId, validFrom: payload.validFrom, expectedBedVersion: version, reason: null })
      }
      const confirmed = await bedContextApi.on(garden, bed.id, payload.validFrom, new AbortController().signal)
      if (!confirmed.configuration || !confirmed.sectorAssignment) throw new Error('A conclusão ainda não foi confirmada. Retome para conferir o cadastro.')
      releaseNavigationGuard(); saved(confirmed)
    } catch (failure) {
      const normalized = failure instanceof FormFailure ? { message: failure.message, fields: failure.fields, status: undefined } : apiFailure(failure)
      setError(normalized)
      if (!writing && !pending) attempt.current = undefined
      if (normalized.status === 409) setConflict(true)
    } finally { sending.current = false; setBusy(false) }
  }
  return { submit, busy, error, pending, conflict }
}
