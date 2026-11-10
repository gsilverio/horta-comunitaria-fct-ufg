import { useEffect, useId, useRef, useState } from 'react'
import { ImagePlus, Upload } from 'lucide-react'
import { Field } from '../../../components/Fields'
import { Failure } from '../../../components/Feedback'
import type { ApiFailure } from '../../../types/api'
import type { EvidenceDraft } from '../hooks/useEvidenceGallery'

const empty: EvidenceDraft = { file: null, title: '', description: '', capturedOn: '' }

/** Envio de uma foto com prévia local. A prévia não é persistência: só a resposta da API confirma o envio. */
export function EvidenceUploadForm({ busy, progress, error, submit, clearError }: { busy: boolean; progress: number; error?: ApiFailure; submit: (draft: EvidenceDraft) => Promise<boolean>; clearError: () => void }) {
  const [draft, setDraft] = useState<EvidenceDraft>(empty)
  const [preview, setPreview] = useState<string>()
  const current = useRef<string | undefined>(undefined)
  const input = useRef<HTMLInputElement>(null)
  const id = useId(), fileErrors = error?.fields.file
  useEffect(() => () => { if (current.current) URL.revokeObjectURL(current.current) }, [])

  function choose(file: File | null) {
    if (current.current) URL.revokeObjectURL(current.current)
    current.current = file ? URL.createObjectURL(file) : undefined
    setPreview(current.current)
    clearError()
    setDraft(value => ({ ...value, file, title: value.title || (file ? file.name.replace(/\.[^.]+$/, '').slice(0, 120) : '') }))
  }
  async function send(event: React.FormEvent) {
    event.preventDefault()
    if (await submit(draft)) { choose(null); setDraft(empty); if (input.current) input.current.value = '' }
  }

  return <form className="grid gap-4 border border-horta-line bg-white p-4 sm:grid-cols-[minmax(0,14rem)_minmax(0,1fr)]" onSubmit={event => void send(event)} aria-busy={busy}>
    <div className="grid content-start gap-2">
      <label htmlFor={id} className="flex aspect-[4/3] w-full cursor-pointer flex-col items-center justify-center gap-2 overflow-hidden border border-dashed border-[#b8c8bc] bg-horta-canvas text-center text-sm text-horta-muted hover:border-horta-forest focus-within:outline focus-within:outline-2 focus-within:outline-horta-focus">
        {preview ? <img src={preview} alt="Prévia da fotografia selecionada (ainda não salva)" className="h-full w-full object-cover" /> : <><ImagePlus aria-hidden="true" className="size-7 text-horta-forest" /><span>Tirar foto ou escolher arquivo</span><span className="text-xs">JPEG, PNG ou WebP</span></>}
        <input ref={input} id={id} name="file" type="file" accept="image/jpeg,image/png,image/webp" className="sr-only" aria-invalid={!!fileErrors} aria-describedby={`${id}-error`} onChange={event => choose(event.target.files?.[0] ?? null)} />
      </label>
      {preview && <p className="hint">Prévia local — ainda não salva. Fotos grandes são reduzidas antes do envio.</p>}
      <span className="field-error" id={`${id}-error`}>{fileErrors?.join(' ')}</span>
    </div>
    <div className="grid min-w-0 content-start gap-3">
      <Field name="title" label="Título" required value={draft.title} maxLength={120} change={value => setDraft(current => ({ ...current, title: value }))} error={error} hint="Ex.: Pulgões na face inferior das folhas" />
      <Field name="capturedOn" label="Data da foto" type="date" value={draft.capturedOn} change={value => setDraft(current => ({ ...current, capturedOn: value }))} error={error} />
      <Field name="description" label="Descrição" type="textarea" value={draft.description} maxLength={500} change={value => setDraft(current => ({ ...current, description: value }))} error={error} />
      {error && Object.keys(error.fields).length === 0 && <Failure error={error} />}
      <div className="flex flex-wrap items-center gap-3">
        <button type="submit" className="inline-flex items-center gap-2" disabled={busy || !draft.file}><Upload aria-hidden="true" className="size-4" />{busy ? 'Enviando…' : 'Salvar fotografia'}</button>
        {busy && <span role="status" className="text-sm text-horta-muted">{progress > 0 ? `Enviando ${Math.round(progress * 100)}%` : 'Preparando imagem…'}</span>}
        {error && !busy && error.status !== 400 && <span className="text-sm text-horta-muted">O envio pode ser repetido com segurança; a mesma tentativa não duplica a foto.</span>}
      </div>
    </div>
  </form>
}
