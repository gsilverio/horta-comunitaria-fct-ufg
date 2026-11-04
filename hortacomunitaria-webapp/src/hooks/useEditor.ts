import { useRef, useState } from 'react'
import { apiFailure } from '../services/http'
import { FormFailure } from '../features/irrigation/formValues'
import type { ApiFailure } from '../types/api'
import { releaseNavigationGuard } from '../services/navigationGuard'

export function useEditor<T extends { id: string; version: number }, V>(original: T | undefined, create: (values: V, operationId: string) => Promise<T>, correct: (values: V, version: number, reason: string) => Promise<T>, reload: () => Promise<T>, saved: (record: T) => void, idempotent = true) {
  const [baseline, setBaseline] = useState(original), [review, setReview] = useState<T>()
  const [reason, setReason] = useState(''), [error, setError] = useState<ApiFailure>(), [busy, setBusy] = useState(false)
  const [attempt, setAttempt] = useState<{ values: V; operationId: string }>(), [reviewing, setReviewing] = useState(false)
  const [catalogUncertain, setCatalogUncertain] = useState(false)
  const sending = useRef(false)
  async function submit(values: () => V) {
    if (sending.current || catalogUncertain || error?.status === 409 && baseline) return
    setError(undefined)
    try {
      if (baseline && !reason.trim()) throw new FormFailure({ reason: ['Informe o motivo da alteração.'] })
      const payload = attempt?.values ?? values()
      sending.current = true; setBusy(true)
      if (baseline) { const record = await correct(payload, baseline.version, reason.trim()); releaseNavigationGuard(); saved(record) }
      else {
        const current = attempt ?? { values: payload, operationId: crypto.randomUUID() }
        setAttempt(current); const result = await create(current.values, current.operationId); setAttempt(undefined); releaseNavigationGuard(); saved(result)
      }
    } catch (failure) {
      const normalized = failure instanceof FormFailure ? { message: failure.message, fields: failure.fields } : apiFailure(failure)
      setError(normalized)
      if (failure instanceof FormFailure) setAttempt(undefined)
      else if ('status' in normalized && normalized.status && normalized.status >= 400 && normalized.status < 500) setAttempt(undefined)
      else if (!baseline && !idempotent) { setAttempt(undefined); setCatalogUncertain(true) }
    } finally { sending.current = false; setBusy(false) }
  }
  async function reloadConflict() { setReviewing(true); try { setReview(await reload()) } catch (failure) { setError(apiFailure(failure)) } finally { setReviewing(false) } }
  function acceptReview() { if (review && !('validity' in review && review.validity === 'Invalid') && !('isActive' in review && review.isActive === false)) { setBaseline(review); setReview(undefined); setError(undefined) } }
  return { baseline, review, reason, setReason, error, busy, attempt, catalogUncertain, reviewing, submit, reloadConflict, acceptReview }
}
