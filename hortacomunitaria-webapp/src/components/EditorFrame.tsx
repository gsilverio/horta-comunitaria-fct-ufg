import { useGardenState } from "../hooks/useGardenState"
import { useEffect, useRef } from 'react'
import type { ReactNode } from 'react'
import { Failure } from './Feedback'
import { Field } from './Fields'
import type { ApiFailure } from '../types/api'
import { registerNavigationGuard, releaseNavigationGuard } from '../services/navigationGuard'

export function EditorFrame({ title, children, submit, cancel, busy, uncertain, catalogUncertain = false, dependenciesReady = true, requireActiveGarden = true, error, reason, changeReason, conflict, review, acceptReview, submitLabel = 'Salvar registro', cancelLabel = 'Cancelar', description = 'Campos com * são obrigatórios. Valores não informados permanecem desconhecidos.' }: { submitLabel?: string; cancelLabel?: string; description?: string; title: string; children: ReactNode; submit: () => void; cancel: () => void; busy: boolean; uncertain: boolean; catalogUncertain?: boolean; dependenciesReady?: boolean; requireActiveGarden?: boolean; error?: ApiFailure; reason?: string; changeReason?: (value: string) => void; conflict?: () => void; review?: ReactNode; acceptReview?: () => void }) {
  const garden = useGardenState()
  const heading = useRef<HTMLHeadingElement>(null), form = useRef<HTMLFormElement>(null), dirty = useRef(false)
  const navigation = useRef({ busy, uncertain })
  useEffect(() => { navigation.current = { busy, uncertain } }, [busy, uncertain])
  useEffect(() => registerNavigationGuard(() => !navigation.current.busy && !navigation.current.uncertain && (!dirty.current || window.confirm('Descartar este rascunho e sair da tela?'))), [])
  useEffect(() => { const before = (event: BeforeUnloadEvent) => { if (dirty.current || navigation.current.busy || navigation.current.uncertain) event.preventDefault() }; window.addEventListener('beforeunload', before); return () => window.removeEventListener('beforeunload', before) }, [])
  useEffect(() => { heading.current?.focus() }, [])
  useEffect(() => { if (error) { const field = form.current?.querySelector<HTMLElement>('[aria-invalid="true"]'); let parent = field?.parentElement; while (parent) { if (parent instanceof HTMLDetailsElement) parent.open = true; parent = parent.parentElement }; field?.focus() } }, [error])
  function leave() { if (!busy && !uncertain && (!dirty.current || window.confirm('Descartar as alterações não salvas e voltar?'))) { releaseNavigationGuard(); cancel() } }
  return <section className="panel editor-panel"><span className="eyebrow">Cadastro</span><h1 ref={heading} tabIndex={-1}>{title}</h1><p className="muted">{description}</p><form ref={form} onChange={() => { dirty.current = true }} onSubmit={event => { event.preventDefault(); if (dependenciesReady) submit() }}>
    {error && <Failure error={error} />}{uncertain && !busy && <p className="feedback" role="status">{catalogUncertain ? 'O cadastro pode ter sido salvo. Consulte a lista antes de tentar criar novamente; cadastros não têm confirmação por chave de operação.' : 'O resultado do envio é incerto. Confirme a mesma tentativa antes de editar ou sair.'}</p>}
    {catalogUncertain && <button className="secondary" type="button" onClick={() => { releaseNavigationGuard(); cancel() }}>Revisar catálogo antes de novo envio</button>}
    <fieldset disabled={busy || uncertain || requireActiveGarden && !garden.isActive} className="grid min-w-0 gap-5">{children}{changeReason && <Field name="reason" label="Motivo da alteração" type="textarea" required maxLength={500} value={reason || ''} change={changeReason} error={error} />}</fieldset>
    {error?.status === 409 && conflict && <button className="secondary mt-4" type="button" disabled={busy} onClick={conflict}>Recarregar para comparar</button>}
    {review && <section className="my-5 rounded-xl border border-horta-line p-4"><h3>Estado atual do servidor</h3>{review}<p>Seu rascunho foi preservado. Compare os valores antes de reconhecer a versão atual.</p><button type="button" className="secondary" onClick={acceptReview}>Revisei e reconheço a versão atual</button></section>}
    <div className="editor-actions"><button type="button" className="secondary" disabled={busy || uncertain} onClick={leave}>{cancelLabel}</button><button disabled={!dependenciesReady || busy || catalogUncertain || requireActiveGarden && !garden.isActive || (!!conflict && error?.status === 409)}>{busy ? 'Salvando…' : catalogUncertain ? 'Envio pendente de revisão' : uncertain ? 'Confirmar tentativa original' : submitLabel}</button></div>
  </form></section>
}
