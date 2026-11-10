import { RecordTabs } from '../../../components/RecordTabs'
import { useGardenState } from "../../../hooks/useGardenState"
import { useCallback, useEffect, useRef, useState } from 'react'
import { Failure } from '../../../components/Feedback'
import { apiFailure } from '../../../services/http'
import type { ApiFailure } from '../../../types/api'
import { formatInstant, formatNumber, formatVolume, statusLabel } from '../formValues'
import { useRemote } from '../hooks/useRemote'
import { irrigationApi } from '../services/irrigationApi'
import { IrrigationForm } from '../components/IrrigationForm'
import { HistoryPanel } from '../components/HistoryPanel'
import type { CatalogItem, IrrigationDetail, Participant } from '../types/irrigation'

export function IrrigationDetailPage({ garden, id, back, changed, sectors, participants }: { garden: string; id: string; back: () => void; changed: () => void; sectors: CatalogItem[]; participants: Participant[] }) {
  const workspace = useGardenState()
  const [revision, setRevision] = useState(0), [historyPage, setHistoryPage] = useState(1)
  const [editing, setEditing] = useState<IrrigationDetail>()
  const [invalidating, setInvalidating] = useState(false), [reason, setReason] = useState('')
  const [busy, setBusy] = useState(false), [error, setError] = useState<ApiFailure>(), [success, setSuccess] = useState('')
  const sending = useRef(false), title = useRef<HTMLHeadingElement>(null)
  useEffect(() => { if (!editing) title.current?.focus() }, [editing])
  const load = useCallback((signal: AbortSignal) => irrigationApi.detail(garden, id, signal), [garden, id])
  const record = useRemote(`${id}/${revision}`, load)
  const historyLoad = useCallback((signal: AbortSignal) => irrigationApi.history(garden, id, historyPage, signal), [garden, id, historyPage])
  const history = useRemote(`${id}/${revision}/${historyPage}`, historyLoad)
  const refresh = () => { setRevision(value => value + 1); setError(undefined) }
  function saved(detail: IrrigationDetail) {
    setEditing(undefined); setInvalidating(false); setReason(''); setHistoryPage(1); refresh(); changed()
    setSuccess(detail.validity === 'Invalid' ? 'Irrigação invalidada. O histórico foi preservado.' : 'Registro salvo. Dados e indicador serão recarregados.')
  }
  async function invalidate() {
    if (sending.current || !record.data) return
    if (!reason.trim() || reason.trim().length > 500) { setError({ message: 'Informe um motivo entre 1 e 500 caracteres.', fields: { reason: ['Motivo obrigatório.'] } }); return }
    sending.current = true; setBusy(true); setError(undefined)
    try { saved(await irrigationApi.invalidate(garden, id, record.data.version, reason.trim())) }
    catch (failure) { setError(apiFailure(failure)) }
    finally { sending.current = false; setBusy(false) }
  }
  if (editing) return <IrrigationForm garden={garden} original={editing} saved={saved} cancel={() => setEditing(undefined)} />
  const detail = record.data
  return <>
    <button className="text-button" onClick={back} disabled={busy}>← Voltar aos registros</button>
    {success && <p className="feedback success" role="status">{success}</p>}
    <h1 className="mb-3" id="detail-title" ref={title} tabIndex={-1}>Detalhe da irrigação</h1>
    <RecordTabs label="Seções da irrigação" tabs={[{ id: 'dados', label: 'Dados', content: <section className="panel" aria-labelledby="detail-title">
      {record.loading && <p role="status">Carregando registro…</p>}{record.error && <Failure error={record.error} retry={refresh} />}
      {detail && <><div className="flex flex-wrap items-start justify-between gap-3"><div><h2 className="mb-0.5">{detail.sectorName}</h2><p>{formatInstant(detail.startedAt)} · São Paulo</p></div><span className={`badge ${detail.validity === 'Invalid' ? 'invalid' : ''}`}>{detail.validity === 'Invalid' ? 'Invalidado' : statusLabel(detail.status)}</span></div>
        <dl className="facts detail-facts"><div><dt>Volume</dt><dd>{formatVolume(detail.volumeLiters)}</dd></div><div><dt>Origem / método</dt><dd>{detail.volumeSource === 'Estimated' ? 'Estimado' : detail.volumeSource === 'Measured' ? 'Medido' : 'Não informada'} · {detail.volumeMethod ?? 'Não informado'}</dd></div>
          <div><dt>Fim</dt><dd>{detail.endedAt ? formatInstant(detail.endedAt) : 'Não informado'}</dd></div><div><dt>Duração ativa</dt><dd>{detail.activeDurationMinutes === null ? 'Não informada' : `${formatNumber(detail.activeDurationMinutes)} min`}</dd></div><div><dt>Vazão informativa</dt><dd>{detail.flowRateLitersPerMinute === null ? 'Não informada' : `${formatNumber(detail.flowRateLitersPerMinute)} L/min`}</dd></div>
          <div><dt>Canteiros atendidos</dt><dd>{detail.beds.length ? detail.beds.map(bed => bed.name).join(', ') : 'Não informados'}</dd></div><div><dt>Responsável pelo trabalho</dt><dd>{participants.find(person => person.id === detail.responsibleParticipantId)?.displayName ?? (detail.responsibleParticipantId ? 'Referência histórica indisponível no catálogo ativo' : 'Não informado')}</dd></div><div><dt>Observações</dt><dd>{detail.notes ?? 'Não informadas'}</dd></div>
          <div><dt>Registrado por</dt><dd>{detail.recordedBy.displayName}{detail.recordedBy.isDemonstration && ' · autoria demonstrativa'}</dd></div><div><dt>Incluído em / versão</dt><dd>{formatInstant(detail.recordedAt)} · v{detail.version}</dd></div>{detail.updatedAt && <div><dt>Última alteração</dt><dd>{formatInstant(detail.updatedAt)}</dd></div>}</dl>
        {detail.validity === 'Valid' && workspace.isActive && !invalidating && <div className="mt-6 mb-4 flex flex-wrap justify-end gap-3 max-sm:[&>button]:flex-1"><button className="secondary" onClick={() => setEditing(detail)}>Corrigir registro</button><button className="danger-outline" onClick={() => setInvalidating(true)}>Invalidar registro</button></div>}
        {detail.validity === 'Invalid' && <p className="notice">Preservado para consulta e histórico. Não compõe o consumo conhecido.</p>}
      </>}
      {invalidating && <form className="invalidation" onSubmit={event => { event.preventDefault(); void invalidate() }}><h3>Invalidar com motivo</h3><p>Os dados permanecerão no histórico e sairão do indicador.</p>{error && <Failure error={error} />}
        {error?.status === 409 && <button className="secondary" type="button" onClick={refresh}>Recarregar registro para revisão</button>}
        <label htmlFor="invalidation-reason">Motivo da invalidação *<textarea id="invalidation-reason" required maxLength={500} rows={3} value={reason} disabled={busy} aria-invalid={!!error?.fields.reason} onChange={event => setReason(event.target.value)} /></label>
        <div className="mt-6 mb-4 flex flex-wrap justify-end gap-3 max-sm:[&>button]:flex-1"><button className="secondary" type="button" disabled={busy} onClick={() => setInvalidating(false)}>Cancelar invalidação</button><button className="danger" disabled={busy || !detail || detail.validity === 'Invalid' || error?.status === 409}>{busy ? 'Invalidando…' : 'Confirmar invalidação'}</button></div></form>}
    </section> }, { id: 'historico', label: 'Histórico', content: history.loading ? <p role="status">Carregando histórico…</p> : history.error ? <Failure error={history.error} retry={refresh} /> : history.data && <HistoryPanel data={history.data} changePage={setHistoryPage} sectors={sectors} participants={participants} /> }]} />
  </>
}

