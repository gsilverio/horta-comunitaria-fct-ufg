import { useCallback, useRef, useState } from 'react'
import { useRemote } from '../../../hooks/useRemote'
import { apiFailure } from '../../../services/http'
import type { ApiFailure } from '../../../types/api'
import { evidenceApi } from '../services/evidenceApi'
import { prepareImage } from '../services/prepareImage'
import type { Evidence, EvidenceTarget } from '../types/Evidence'

export interface EvidenceDraft { file: File | null; title: string; description: string; capturedOn: string }

/** Galeria paginada de um registro (ou da horta) com envio idempotente, correção de legenda e invalidação. */
export function useEvidenceGallery(garden: string, scope: { targetType?: EvidenceTarget; targetId?: string; validity?: 'Valid' | 'Invalid' }, pageSize = 12) {
  const [page, setPage] = useState(1), [revision, setRevision] = useState(0)
  const key = JSON.stringify({ garden, ...scope, page, pageSize, revision })
  const { targetType, targetId, validity } = scope
  const load = useCallback((signal: AbortSignal) => evidenceApi.list(garden, { targetType, targetId, validity, page, pageSize }, signal), [garden, targetType, targetId, validity, page, pageSize])
  const list = useRemote(key, load)
  const reload = () => setRevision(value => value + 1)

  // Uma tentativa lógica de envio preserva a mesma operationId e o mesmo conteúdo até ser confirmada.
  const attempt = useRef<{ operationId: string; file: File; prepared: { blob: Blob; fileName: string } } | null>(null)
  const sending = useRef(false)
  const [uploading, setUploading] = useState(false), [progress, setProgress] = useState(0), [uploadError, setUploadError] = useState<ApiFailure>(), [notice, setNotice] = useState('')

  async function upload(draft: EvidenceDraft): Promise<boolean> {
    if (sending.current || !targetType || !targetId) return false
    if (!draft.file) { setUploadError({ message: 'Selecione uma fotografia.', fields: { file: ['Selecione uma fotografia.'] } }); return false }
    if (!draft.title.trim()) { setUploadError({ message: 'Informe um título curto.', fields: { title: ['Informe um título curto.'] } }); return false }
    sending.current = true; setUploading(true); setUploadError(undefined); setNotice(''); setProgress(0)
    try {
      if (!attempt.current || attempt.current.file !== draft.file) attempt.current = { operationId: crypto.randomUUID(), file: draft.file, prepared: await prepareImage(draft.file) }
      const { operationId, prepared } = attempt.current
      await evidenceApi.upload(garden, { file: prepared.blob, fileName: prepared.fileName, targetType, targetId, title: draft.title.trim(), description: draft.description.trim() || null, capturedOn: draft.capturedOn || null }, operationId, setProgress)
      attempt.current = null
      setNotice('Fotografia salva.')
      setPage(1); reload()
      return true
    } catch (failure) {
      const error = apiFailure(failure)
      // Rejeições de validação não criaram nada: a próxima tentativa pode usar outra chave.
      if (error.status === 400) attempt.current = null
      setUploadError(error)
      return false
    } finally { sending.current = false; setUploading(false) }
  }

  const [changing, setChanging] = useState(false), [changeError, setChangeError] = useState<ApiFailure>()
  async function change(run: () => Promise<Evidence>): Promise<Evidence | undefined> {
    if (changing) return undefined
    setChanging(true); setChangeError(undefined)
    try { const result = await run(); reload(); return result } catch (failure) { setChangeError(apiFailure(failure)); return undefined } finally { setChanging(false) }
  }
  const correct = (item: Evidence, values: { title: string; description: string | null; capturedOn: string | null }, reason: string) => change(() => evidenceApi.correct(garden, item.id, values, item.version, reason))
  const invalidate = (item: Evidence, reason: string) => change(() => evidenceApi.invalidate(garden, item.id, item.version, reason))

  return {
    list, page, setPage, reload, imageUrl: (id: string) => evidenceApi.contentUrl(garden, id),
    upload, uploading, progress, uploadError, clearUploadError: () => setUploadError(undefined), notice,
    correct, invalidate, changing, changeError, clearChangeError: () => setChangeError(undefined),
  }
}
