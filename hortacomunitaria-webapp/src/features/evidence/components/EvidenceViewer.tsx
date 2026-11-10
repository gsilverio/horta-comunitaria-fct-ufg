import { useState } from 'react'
import { ExternalLink } from 'lucide-react'
import { Dialog, DialogContent, DialogDescription, DialogTitle } from '../../../components/ui/dialog'
import { Facts, Field } from '../../../components/Fields'
import { Failure } from '../../../components/Feedback'
import { formatDate } from '../../../services/dates'
import { formatInstant } from '../../irrigation/formValues'
import type { ApiFailure } from '../../../types/api'
import type { Evidence } from '../types/Evidence'
import { evidenceTargets } from '../types/Evidence'

type Mode = 'view' | 'edit' | 'invalidate'
const formatSize = (bytes: number) => bytes < 1024 * 1024
  ? `${Math.max(1, Math.round(bytes / 1024)).toLocaleString('pt-BR')} KB`
  : `${(bytes / 1024 / 1024).toLocaleString('pt-BR', { maximumFractionDigits: 1 })} MB`

/** Visualização ampliada com metadados, correção de legenda (com motivo) e invalidação sem exclusão física. */
export function EvidenceViewer({ item, imageUrl, targetHref, canChange, busy, error, clearError, correct, invalidate, close }: {
  item: Evidence; imageUrl: string; targetHref?: string; canChange: boolean; busy: boolean; error?: ApiFailure; clearError: () => void
  correct: (values: { title: string; description: string | null; capturedOn: string | null }, reason: string) => Promise<boolean>
  invalidate: (reason: string) => Promise<boolean>; close: () => void
}) {
  const [mode, setMode] = useState<Mode>('view')
  const [draft, setDraft] = useState({ title: item.title, description: item.description ?? '', capturedOn: item.capturedOn ?? '' })
  const [reason, setReason] = useState('')
  const valid = item.validity === 'Valid'
  function switchTo(next: Mode) { clearError(); setReason(''); setMode(next) }
  async function save() {
    if (mode === 'edit' && await correct({ title: draft.title.trim(), description: draft.description.trim() || null, capturedOn: draft.capturedOn || null }, reason.trim())) close()
    if (mode === 'invalidate' && await invalidate(reason.trim())) close()
  }
  return <Dialog open onOpenChange={open => { if (!open && !busy) close() }}>
    <DialogContent className="w-[calc(100%-2rem)] max-w-4xl gap-0 overflow-y-auto p-0">
      <div className="bg-[#1f2a22]"><img src={imageUrl} alt={item.description ?? item.title} className="mx-auto max-h-[60vh] w-auto max-w-full object-contain" /></div>
      <div className="grid gap-4 p-4 sm:p-5">
        <div className="pr-10">
          <DialogTitle className="text-lg font-semibold text-horta-ink">{item.title}</DialogTitle>
          <DialogDescription>{evidenceTargets[item.targetType].label}{!valid && ' · Fotografia invalidada, mantida para rastreabilidade'}</DialogDescription>
        </div>
        {mode === 'view' && <>
          <Facts items={[
            { label: 'Descrição', value: item.description ?? 'Não informado' },
            { label: 'Data da foto', value: item.capturedOn ? formatDate(item.capturedOn) : 'Não informado' },
            { label: 'Enviada em', value: formatInstant(item.recordedAt) },
            { label: 'Autor técnico (demonstração)', value: item.recordedByUserId },
            { label: 'Arquivo', value: `${item.originalFileName} · ${formatSize(item.sizeBytes)}` },
          ]} />
          <div className="flex flex-wrap gap-2">
            {targetHref && <a className="action-link secondary" href={targetHref} onClick={close}><ExternalLink aria-hidden="true" className="size-4" />Abrir registro vinculado</a>}
            <a className="action-link secondary" href={imageUrl} target="_blank" rel="noopener">Abrir em tamanho original</a>
            {valid && canChange && <><button type="button" className="secondary" onClick={() => switchTo('edit')}>Corrigir legenda</button><button type="button" className="danger" onClick={() => switchTo('invalidate')}>Invalidar foto</button></>}
          </div>
        </>}
        {mode !== 'view' && <form className="grid gap-3" onSubmit={event => { event.preventDefault(); void save() }}>
          {mode === 'edit' && <>
            <Field name="title" label="Título" required value={draft.title} maxLength={120} change={value => setDraft(current => ({ ...current, title: value }))} error={error} />
            <Field name="capturedOn" label="Data da foto" type="date" value={draft.capturedOn} change={value => setDraft(current => ({ ...current, capturedOn: value }))} error={error} />
            <Field name="description" label="Descrição" type="textarea" value={draft.description} maxLength={500} change={value => setDraft(current => ({ ...current, description: value }))} error={error} />
          </>}
          {mode === 'invalidate' && <p className="text-sm text-horta-muted">A foto sai da galeria, mas o arquivo e o histórico são preservados.</p>}
          <Field name="reason" label="Motivo" required type="textarea" value={reason} maxLength={500} change={setReason} error={error} />
          {error && <Failure error={error} />}
          <div className="flex flex-wrap gap-2">
            <button type="submit" className={mode === 'invalidate' ? 'danger' : undefined} disabled={busy || !reason.trim()}>{busy ? 'Salvando…' : mode === 'edit' ? 'Salvar correção' : 'Confirmar invalidação'}</button>
            <button type="button" className="secondary" disabled={busy} onClick={() => switchTo('view')}>Cancelar</button>
          </div>
        </form>}
      </div>
    </DialogContent>
  </Dialog>
}
