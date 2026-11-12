import { linkTo } from '../../../routes/routes'
import { Group, RequiredMark } from '../../../components/Fields'
import { useEffect, useRef, useState } from 'react'
import { Failure } from '../../../components/Feedback'
import { useIrrigationForm } from '../hooks/useIrrigationForm'
import { formatVolume } from '../formValues'
import type { Draft } from '../formValues'
import type { IrrigationDetail, Status, VolumeSource } from '../types/irrigation'
import { discardDraft, registerNavigationGuard, releaseNavigationGuard } from '../../../services/navigationGuard'
import { confirmAction } from '../../../services/confirmation'

export function IrrigationForm({ garden, original, saved, cancel }: {
  garden: string; original?: IrrigationDetail; saved: (detail: IrrigationDetail) => void; cancel: () => void
}) {
  const form = useIrrigationForm(garden, original, saved)
  const title = useRef<HTMLHeadingElement>(null)
  useEffect(() => { if (!form.context.loading) title.current?.focus() }, [form.context.loading])
  const { draft, change, context, beds, error } = form
  const locked = form.busy || !!form.attempt
  const [initial] = useState(() => JSON.stringify(draft))
  const dirty = initial !== JSON.stringify(draft) || !!form.reason.trim()
  const navigation = useRef({ locked, dirty })
  useEffect(() => { navigation.current = { locked, dirty } }, [locked, dirty])
  useEffect(() => registerNavigationGuard(() => navigation.current.locked ? false : navigation.current.dirty ? discardDraft('O registro de irrigação ainda não foi salvo e será perdido.') : true), [])
  useEffect(() => { const before = (event: BeforeUnloadEvent) => { if (dirty || locked) event.preventDefault() }; window.addEventListener('beforeunload', before); return () => window.removeEventListener('beforeunload', before) }, [dirty, locked])
  async function leave() { if (locked) return; if (dirty && !await confirmAction(discardDraft('O registro de irrigação ainda não foi salvo e será perdido.'))) return; releaseNavigationGuard(); cancel() }
  const fields = error?.fields ?? {}
  const control = (name: string) => ({ id: `field-${name}`, 'aria-invalid': !!fields[name], 'aria-describedby': fields[name] ? `error-${name}` : undefined })
  const message = (name: string) => fields[name] && <span className="field-error" id={`error-${name}`}>{fields[name].join(' ')}</span>
  const numberField = (name: 'activeDurationMinutes' | 'volumeLiters' | 'flowRateLitersPerMinute', label: string) =>
    <label htmlFor={`field-${name}`}>{label}<input {...control(name)} inputMode="decimal" placeholder="Não informado" value={draft[name]} onChange={event => change(name, event.target.value)} />{message(name)}</label>
  const toggleBed = (id: string) => change('bedIds', draft.bedIds.includes(id) ? draft.bedIds.filter(value => value !== id) : [...draft.bedIds, id])
  const unavailable = original?.beds.filter(bed => draft.bedIds.includes(bed.id) && beds.data && !beds.data.some(option => option.id === bed.id)) ?? []
  function textField(name: keyof Pick<Draft, 'startedAt' | 'endedAt'>, label: string, required = false) {
    return <label htmlFor={`field-${name}`}>{label}{required && <RequiredMark />}<input {...control(name)} type="datetime-local" step="0.001" required={required} value={draft[name]} onChange={event => change(name, event.target.value)} />{message(name)}</label>
  }
  return <section className="panel editor-panel" aria-labelledby="form-title">
    <div className="flex flex-wrap items-start justify-between gap-3"><div><h2 className="text-2xl font-semibold" id="form-title" ref={title} tabIndex={-1}>{original ? 'Corrigir irrigação' : 'Nova irrigação'}</h2></div></div>
    <p className="muted">Informe o que foi coletado. Campos vazios permanecem desconhecidos.</p>
    {context.error && <Failure error={context.error} retry={form.retryCatalogs} />}
    {error && <Failure error={error} />}
    {error?.status === 409 && original && <button className="secondary" disabled={form.reviewing} onClick={() => void form.reloadConflict()}>Recarregar registro para revisão</button>}
    {form.review && <div className="feedback" role="status"><strong>Estado atual no servidor: versão {form.review.version}</strong><p>Volume: {formatVolume(form.review.volumeLiters)} · {form.review.validity === 'Invalid' ? 'Invalidado' : 'Válido'}. Seu rascunho foi preservado.</p>
      {form.review.validity === 'Valid' ? <button type="button" className="secondary" onClick={form.applyReview}>Revisei o estado; usar esta versão</button> : <p>Este registro foi invalidado e não pode mais ser corrigido.</p>}
      <details><summary>Conferir valores atuais</summary><p>Setor: {form.review.sectorName}. Início: {form.review.startedAt}. Método: {form.review.volumeMethod ?? 'Não informado'}. Observações: {form.review.notes ?? 'Não informadas'}.</p></details></div>}
    {form.attempt && !form.busy && <div className="feedback" role="status">O resultado do envio ainda é incerto. Tente novamente com os mesmos dados para confirmar o registro; a chave da tentativa foi preservada.</div>}
    <form onSubmit={event => { event.preventDefault(); void form.submit() }}>
      <fieldset disabled={locked} className="grid min-w-0 gap-5">
        <legend className="sr-only">Dados da irrigação</legend>
        <Group title="Evento" hint="Horários em São Paulo (America/Sao_Paulo), independentemente do fuso do navegador.">
          <label htmlFor="field-sectorId">Setor<RequiredMark /><select {...control('sectorId')} required value={draft.sectorId} onChange={event => change('sectorId', event.target.value)} disabled={context.loading}>
            <option value="">Selecione o setor</option>{context.data?.sectors.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
            {original && !context.data?.sectors.some(item => item.id === draft.sectorId) && draft.sectorId && <option value={draft.sectorId}>{original.sectorName} (registro histórico)</option>}
          </select>{message('sectorId')}</label>
          <label htmlFor="field-status">Situação<RequiredMark /><select {...control('status')} value={draft.status} onChange={event => change('status', event.target.value as Status)}><option value="Completed">Concluída</option><option value="Interrupted">Interrompida</option></select></label>
          {textField('startedAt', 'Início', true)}{textField('endedAt', 'Fim')}
        </Group>
        <fieldset className="form-section" disabled={beds.loading || !!beds.error}><legend className="sr-only">Canteiros atendidos</legend>
          <div aria-hidden="true" className="form-section-heading"><strong>Canteiros atendidos</strong><span>Seleção opcional. O volume é do evento inteiro.</span></div>
          <div className="grid min-w-0 gap-2">
          {beds.loading ? <p role="status">Buscando canteiros…</p> : <div className="bed-options">{beds.data?.map(bed => <label className="check-card" key={bed.id}><input type="checkbox" checked={draft.bedIds.includes(bed.id)} onChange={() => toggleBed(bed.id)} /><span><strong>{bed.name}</strong></span></label>)}</div>}
          {!beds.loading && beds.data?.length === 0 && <p className="muted">Nenhum canteiro associado a este setor nesta data. Você pode registrar a irrigação somente pelo setor ou <a className="underline" href={linkTo(garden, 'horta/mapa')}>configurar a associação dos canteiros</a>.</p>}
          {unavailable.map(bed => <label key={bed.id} className="check-card"><input type="checkbox" checked onChange={() => toggleBed(bed.id)} />{bed.name}: indisponível no contexto atual; desmarque para revisar.</label>)}
          {message('bedIds')}
          </div>
        </fieldset>
        {beds.error && <Failure error={beds.error} retry={form.retryCatalogs} />}
        <Group title="Água e duração" hint="O método é obrigatório quando houver volume.">{numberField('volumeLiters', 'Volume (L)')}{numberField('activeDurationMinutes', 'Duração ativa (min)')}{numberField('flowRateLitersPerMinute', 'Vazão informativa (L/min)')}
          <label htmlFor="field-volumeSource">Origem do volume<select {...control('volumeSource')} disabled={!draft.volumeLiters.trim()} value={draft.volumeSource} onChange={event => change('volumeSource', event.target.value as VolumeSource)}><option value="Measured">Medido</option><option value="Estimated">Estimado</option></select>{message('volumeSource')}</label>
          <label className="col-span-full" htmlFor="field-volumeMethod">Método de obtenção do volume{draft.volumeLiters.trim() && <RequiredMark />}<input {...control('volumeMethod')} maxLength={200} disabled={!draft.volumeLiters.trim()} value={draft.volumeMethod} placeholder="Ex.: recipiente graduado" onChange={event => change('volumeMethod', event.target.value)} />{message('volumeMethod')}</label>
        </Group>
        <Group title="Registro">
          <label className="col-span-full" htmlFor="field-responsibleParticipantId">Responsável pelo trabalho<select {...control('responsibleParticipantId')} value={draft.responsibleParticipantId} disabled={context.loading} onChange={event => change('responsibleParticipantId', event.target.value)}><option value="">Não informado</option>{context.data?.participants.map(item => <option key={item.id} value={item.id}>{item.displayName}</option>)}
            {draft.responsibleParticipantId && !context.data?.participants.some(item => item.id === draft.responsibleParticipantId) && <option value={draft.responsibleParticipantId}>Responsável do registro histórico (indisponível)</option>}
          </select>{message('responsibleParticipantId')}</label>
          <label className="col-span-full" htmlFor="field-notes">Observações<textarea {...control('notes')} rows={3} maxLength={1000} value={draft.notes} onChange={event => change('notes', event.target.value)} />{message('notes')}</label>
          {original && <label className="col-span-full" htmlFor="field-reason">Motivo da correção<RequiredMark /><textarea {...control('reason')} rows={2} required maxLength={500} value={form.reason} onChange={event => form.setReason(event.target.value)} />{message('reason')}</label>}
        </Group>
      </fieldset>
      <div className="editor-actions"><button type="button" className="secondary" disabled={locked} onClick={leave}>Cancelar</button><button type="submit" disabled={form.busy || context.loading || beds.loading || !!context.error || !!beds.error || error?.status === 409 || form.review?.validity === 'Invalid'}>
        {form.busy ? 'Salvando…' : form.attempt ? 'Confirmar tentativa anterior' : original ? 'Salvar correção' : 'Salvar irrigação'}</button></div>
      <p className="hint">Autoria: pessoa autenticada nesta sessão. O responsável selecionado não altera a autoria.</p>
    </form>
  </section>
}

