import { useCallback, useRef, useState } from 'react'
import type { ApiFailure } from '../../../types/api'
import { apiFailure } from '../../../services/http'
import { editDraft, emptyDraft, FormFailure, rememberedWaterPrice, rememberWaterPrice, toValues } from '../formValues'
import type { Draft } from '../formValues'
import { irrigationApi } from '../services/irrigationApi'
import type { IrrigationDetail, IrrigationValues } from '../types/irrigation'
import { useRemote } from './useRemote'
import { releaseNavigationGuard } from '../../../services/navigationGuard'

export function useIrrigationForm(garden: string, original: IrrigationDetail | undefined, saved: (detail: IrrigationDetail) => void) {
  const [draft, setDraft] = useState<Draft>(() => original ? editDraft(original) : emptyDraft(rememberedWaterPrice(garden)))
  const [reason, setReason] = useState('')
  const [baseline, setBaseline] = useState(original)
  const [review, setReview] = useState<IrrigationDetail>()
  const [reviewing, setReviewing] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<ApiFailure>()
  const [attempt, setAttempt] = useState<{ id: string; values: IrrigationValues }>()
  const [revision, setRevision] = useState(0)
  const sending = useRef(false)
  const contextLoad = useCallback((signal: AbortSignal) => Promise.all([irrigationApi.sectors(garden, signal), irrigationApi.participants(garden, signal)]).then(([sectors, participants]) => ({ sectors, participants })), [garden])
  const context = useRemote(`${garden}/${revision}`, contextLoad)
  const on = draft.startedAt.slice(0, 10)
  const bedsLoad = useCallback((signal: AbortSignal) => draft.sectorId && on ? irrigationApi.beds(garden, draft.sectorId, on, signal) : Promise.resolve([]), [garden, draft.sectorId, on])
  const beds = useRemote(`${garden}/${draft.sectorId}/${on}/${revision}`, bedsLoad)
  function change<K extends keyof Draft>(field: K, value: Draft[K]) {
    if (busy || attempt) return
    setDraft(current => ({ ...current, [field]: value,
      bedIds: field === 'sectorId' || (field === 'startedAt' && String(value).slice(0, 10) !== current.startedAt.slice(0, 10)) ? [] : field === 'bedIds' ? value as string[] : current.bedIds }))
    setError(undefined)
  }
  async function submit() {
    if (sending.current) return
    setError(undefined)
    try {
      const values = attempt?.values ?? toValues(draft, baseline)
      if (original && (!reason.trim() || reason.trim().length > 500)) throw new FormFailure({ reason: ['Informe um motivo entre 1 e 500 caracteres.'] })
      if (!attempt && values.bedIds.some(id => !beds.data?.some(bed => bed.id === id))) throw new FormFailure({ bedIds: ['Revise os canteiros que não estão disponíveis no contexto atual.'] })
      if (!attempt && (context.loading || beds.loading || context.error || beds.error)) throw new Error('Aguarde ou recarregue os catálogos antes de salvar.')
      sending.current = true
      setBusy(true)
      let detail: IrrigationDetail
      if (baseline) detail = await irrigationApi.correct(garden, baseline.id, values, baseline.version, reason.trim())
      else {
        const current = attempt ?? { id: crypto.randomUUID(), values }
        setAttempt(current)
        detail = await irrigationApi.create(garden, current.values, current.id)
        rememberWaterPrice(garden, draft.waterPrice)
      }
      setAttempt(undefined)
      releaseNavigationGuard()
      saved(detail)
    } catch (failure) {
      if (failure instanceof FormFailure) setError({ message: failure.message, fields: failure.fields })
      else {
        const normalized = apiFailure(failure)
        setError(normalized)
        // Falha 4xx confirma rejeição; rede/timeout/5xx podem ter ocorrido após a gravação.
        if (normalized.status && normalized.status >= 400 && normalized.status < 500) setAttempt(undefined)
      }
    } finally { sending.current = false; setBusy(false) }
  }
  async function reloadConflict() {
    if (!baseline) return
    setReviewing(true)
    try { setReview(await irrigationApi.detail(garden, baseline.id)) } catch (failure) { setError(apiFailure(failure)) }
    finally { setReviewing(false) }
  }
  function applyReview() {
    if (!review || review.validity === 'Invalid') return
    setBaseline(review); setReview(undefined); setError(undefined)
  }
  return { draft, change, reason, setReason, busy, error, attempt, context, beds, submit, review, reviewing, reloadConflict, applyReview, retryCatalogs: () => setRevision(value => value + 1) }
}
