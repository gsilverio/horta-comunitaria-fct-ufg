import { linkTo } from '../../../routes/routes'
import { FieldLabel, Group } from '../../../components/Fields'
import { HelpButton } from '../../../components/HelpButton'
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
  const numberField = (name: 'activeDurationMinutes' | 'volumeLiters' | 'flowRateLitersPerMinute', label: string, unit: string, hint?: string) =>
    <label htmlFor={`field-${name}`}><FieldLabel label={label} required={false} /><span className="input-with-unit"><input {...control(name)} inputMode="decimal" placeholder="Não informado" value={draft[name]} onChange={event => change(name, event.target.value)} /><span className="input-unit" aria-hidden="true">{unit}</span></span>{hint && <span className="hint">{hint}</span>}{message(name)}</label>
  const toggleBed = (id: string) => change('bedIds', draft.bedIds.includes(id) ? draft.bedIds.filter(value => value !== id) : [...draft.bedIds, id])
  const unavailable = original?.beds.filter(bed => draft.bedIds.includes(bed.id) && beds.data && !beds.data.some(option => option.id === bed.id)) ?? []
  function textField(name: keyof Pick<Draft, 'startedAt' | 'endedAt'>, label: string, required = false) {
    return <label htmlFor={`field-${name}`}><FieldLabel label={label} required={required} /><input {...control(name)} type="datetime-local" step="0.001" required={required} value={draft[name]} onChange={event => change(name, event.target.value)} />{message(name)}</label>
  }
  return <section className="panel editor-panel" aria-labelledby="form-title">
    <div className="page-heading"><div><h2 className="text-2xl font-semibold" id="form-title" ref={title} tabIndex={-1}>{original ? 'Corrigir irrigação' : 'Nova irrigação'}</h2><p>Registre o que foi observado em campo.</p></div></div>
    {context.error && <Failure error={context.error} retry={form.retryCatalogs} />}
    {error && <Failure error={error} />}
    {error?.status === 409 && original && <button className="secondary" disabled={form.reviewing} onClick={() => void form.reloadConflict()}>Recarregar registro para revisão</button>}
    {form.review && <div className="feedback" role="status"><strong>Estado atual no servidor: versão {form.review.version}</strong><p>Volume: {formatVolume(form.review.volumeLiters)} · {form.review.validity === 'Invalid' ? 'Invalidado' : 'Válido'}. Seu rascunho foi preservado.</p>
      {form.review.validity === 'Valid' ? <button type="button" className="secondary" onClick={form.applyReview}>Revisei o estado; usar esta versão</button> : <p>Este registro foi invalidado e não pode mais ser corrigido.</p>}
      <details><summary>Conferir valores atuais</summary><p>Setor: {form.review.sectorName}. Início: {form.review.startedAt}. Método: {form.review.volumeMethod ?? 'Não informado'}. Observações: {form.review.notes ?? 'Não informadas'}.</p></details></div>}
    {form.attempt && !form.busy && <div className="feedback" role="status">O resultado do envio ainda é incerto. Tente novamente com os mesmos dados para confirmar o registro; a chave da tentativa foi preservada.</div>}
    <form onSubmit={event => { event.preventDefault(); void form.submit() }}>
      <fieldset disabled={locked} className="form-stack">
        <legend className="sr-only">Dados da irrigação</legend>
        <Group title="Quando e onde" help={<><p>Os horários seguem o fuso de São Paulo, qualquer que seja o fuso do seu dispositivo.</p><p>O fim é opcional: deixe vazio se não souber quando a irrigação terminou.</p><p>Campos vazios ficam registrados como não informados, nunca como zero.</p></>}>
          <label htmlFor="field-sectorId"><FieldLabel label="Setor" required /><select {...control('sectorId')} required value={draft.sectorId} onChange={event => change('sectorId', event.target.value)} disabled={context.loading}>
            <option value="">Selecione o setor</option>{context.data?.sectors.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
            {original && !context.data?.sectors.some(item => item.id === draft.sectorId) && draft.sectorId && <option value={draft.sectorId}>{original.sectorName} (registro histórico)</option>}
          </select>{message('sectorId')}</label>
          <label htmlFor="field-status"><FieldLabel label="Situação" required /><select {...control('status')} value={draft.status} onChange={event => change('status', event.target.value as Status)}><option value="Completed">Concluída</option><option value="Interrupted">Interrompida</option></select></label>
          {textField('startedAt', 'Início', true)}{textField('endedAt', 'Fim')}
        </Group>
        {/* Os canteiros dependem do setor: a seção só aparece depois de escolhê-lo. */}
        {draft.sectorId && <fieldset className="form-section" disabled={beds.loading || !!beds.error}><legend className="sr-only">Canteiros atendidos</legend>
          <div className="form-section-heading"><span className="form-section-title"><strong aria-hidden="true">Canteiros atendidos</strong><HelpButton title="Canteiros atendidos"><p>Opcional. Marque os canteiros do setor que receberam água.</p><p>O volume vale para a irrigação inteira e não é dividido entre os canteiros marcados.</p></HelpButton></span></div>
          <div className="grid min-w-0 gap-2">
          {beds.loading ? <p role="status">Buscando canteiros…</p> : <div className="bed-options">{beds.data?.map(bed => <label className="check-card" key={bed.id}><input type="checkbox" checked={draft.bedIds.includes(bed.id)} onChange={() => toggleBed(bed.id)} /><span><strong>{bed.name}</strong></span></label>)}</div>}
          {!beds.loading && beds.data?.length === 0 && <p className="muted m-0">Nenhum canteiro associado a este setor nesta data. Registre só pelo setor ou <a className="underline" href={linkTo(garden, 'horta/mapa')}>associe canteiros no mapa</a>.</p>}
          {unavailable.map(bed => <label key={bed.id} className="check-card"><input type="checkbox" checked onChange={() => toggleBed(bed.id)} />{bed.name}: indisponível no contexto atual; desmarque para revisar.</label>)}
          {message('bedIds')}
          </div>
        </fieldset>}
        {beds.error && <Failure error={beds.error} retry={form.retryCatalogs} />}
        <Group title="Volume de água" help={<><p>Deixe o volume vazio se ele não foi medido nem estimado.</p><p>Com volume informado, a origem e o método passam a ser obrigatórios:</p><ul><li><strong>Medido</strong>: lido em hidrômetro ou recipiente graduado.</li><li><strong>Estimado</strong>: calculado, por exemplo tempo × vazão.</li></ul></>}>
          {numberField('volumeLiters', 'Volume (L)', 'L')}
          <label htmlFor="field-volumeSource"><FieldLabel label="Origem do volume" required={Boolean(draft.volumeLiters.trim())} /><select {...control('volumeSource')} disabled={!draft.volumeLiters.trim()} value={draft.volumeSource} onChange={event => change('volumeSource', event.target.value as VolumeSource)}><option value="Measured">Medido</option><option value="Estimated">Estimado</option></select>{message('volumeSource')}</label>
          <label className="col-span-full" htmlFor="field-volumeMethod"><FieldLabel label="Método de obtenção do volume" required={Boolean(draft.volumeLiters.trim())} /><input {...control('volumeMethod')} maxLength={200} disabled={!draft.volumeLiters.trim()} value={draft.volumeMethod} placeholder="Ex.: recipiente graduado" onChange={event => change('volumeMethod', event.target.value)} />{message('volumeMethod')}</label>
        </Group>
        <Group title="Duração e vazão" help={<><p>Ambos são opcionais.</p><p><strong>Duração ativa</strong>: tempo com água saindo, sem contar pausas.</p><p><strong>Vazão</strong>: apenas informativa; não é usada para calcular o volume.</p></>}>
          {numberField('activeDurationMinutes', 'Duração ativa (min)', 'min')}
          {numberField('flowRateLitersPerMinute', 'Vazão informativa (L/min)', 'L/min')}
        </Group>
        {!original && <Group title="Custo da água" help={<><p>Opcional. Com volume informado, o custo da água é lançado em Custos automaticamente.</p><p>O último valor usado fica sugerido neste navegador.</p></>}>
          <label htmlFor="field-waterPrice"><FieldLabel label="Valor do m³ de água" required={false} /><span className="input-with-unit"><input {...control('waterPrice')} inputMode="decimal" placeholder="Não informado" value={draft.waterPrice} onChange={event => change('waterPrice', event.target.value)} /><span className="input-unit" aria-hidden="true">R$/m³</span></span>{message('waterPrice')}</label>
        </Group>}
        <Group title="Registro" help={<p>O responsável é quem fez o trabalho em campo. A autoria do registro é sempre a pessoa conectada e não muda com o responsável escolhido.</p>}>
          <label className="col-span-full" htmlFor="field-responsibleParticipantId"><FieldLabel label="Responsável pelo trabalho" required={false} /><select {...control('responsibleParticipantId')} value={draft.responsibleParticipantId} disabled={context.loading} onChange={event => change('responsibleParticipantId', event.target.value)}><option value="">Não informado</option>{context.data?.participants.map(item => <option key={item.id} value={item.id}>{item.displayName}</option>)}
            {draft.responsibleParticipantId && !context.data?.participants.some(item => item.id === draft.responsibleParticipantId) && <option value={draft.responsibleParticipantId}>Responsável do registro histórico (indisponível)</option>}
          </select>{message('responsibleParticipantId')}</label>
          <label className="col-span-full" htmlFor="field-notes"><FieldLabel label="Observações" required={false} /><textarea {...control('notes')} rows={3} maxLength={1000} value={draft.notes} onChange={event => change('notes', event.target.value)} />{message('notes')}</label>
          {original && <label className="col-span-full" htmlFor="field-reason"><FieldLabel label="Motivo da correção" required /><textarea {...control('reason')} rows={2} required maxLength={500} value={form.reason} onChange={event => form.setReason(event.target.value)} />{message('reason')}</label>}
        </Group>
      </fieldset>
      <div className="editor-actions"><button type="button" className="secondary" disabled={locked} onClick={leave}>Cancelar</button><button type="submit" disabled={form.busy || context.loading || beds.loading || !!context.error || !!beds.error || error?.status === 409 || form.review?.validity === 'Invalid'}>
        {form.busy ? 'Salvando…' : form.attempt ? 'Confirmar tentativa anterior' : original ? 'Salvar correção' : 'Salvar irrigação'}</button></div>
    </form>
  </section>
}

