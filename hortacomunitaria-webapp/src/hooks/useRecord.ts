import { useCallback, useRef, useState } from 'react'
import { useRemote } from './useRemote'
import { apiFailure } from '../services/http'
import type { ApiFailure, Page } from '../types/api'
import type { AuditEntry } from '../types/records'
export function useRecord<T>(id: string, get: (signal: AbortSignal) => Promise<T>, audit: (page: number, signal: AbortSignal) => Promise<Page<AuditEntry>>, change: (version: number, reason: string) => Promise<T>, confirmed: () => void) {
  const [revision, setRevision] = useState(0), [page, setPage] = useState(1), [busy, setBusy] = useState(false), [reason, setReason] = useState(''), [error, setError] = useState<ApiFailure>(), [notice, setNotice] = useState('')
  const loadHistory = useCallback((signal: AbortSignal) => audit(page, signal), [audit, page])
  const sending = useRef(false), [reviewReady, setReviewReady] = useState(false)
  const detail = useRemote(`${id}/${revision}`, get), history = useRemote(`${id}/${page}/${revision}`, loadHistory)
  const refresh = () => { setRevision(x => x + 1); if (error?.status === 409) setReviewReady(true); else setError(undefined) }
  async function deactivate(version: number) { if (sending.current || error?.status === 409) return; if (!reason.trim()) { setError({ message: 'Informe o motivo.', fields: { reason: ['Motivo obrigatório.'] } }); return }; sending.current = true; setBusy(true); try { await change(version, reason.trim()); setNotice('Alteração confirmada. O registro e o histórico foram preservados.'); setReason(''); refresh(); confirmed() } catch (failure) { setError(apiFailure(failure)); setReviewReady(false) } finally { sending.current = false; setBusy(false) } }
  return { detail, history, page, setPage, busy, reason, setReason, error, notice, refresh, deactivate, reviewReady, acknowledge: () => { setError(undefined); setReviewReady(false) } }
}
