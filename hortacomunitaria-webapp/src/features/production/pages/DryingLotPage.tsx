import type { AppRoute } from '../../../routes/routes'
import { goToSame } from '../../../routes/navigation'
import { linkTo, samePage } from '../../../routes/routes'
import { choiceLabel } from '../../../services/lookups'
import { formatDate } from '../../../services/dates'
import { formatNumber } from '../../irrigation/formValues'
import { EvidenceSection } from "../../evidence/pages/EvidenceSection"
import { RecordRow } from '../../../components/RecordRow'
import { formatInstant } from "../../irrigation/formValues"
import { useCallback } from "react"
import { RecordList } from "../../../components/RecordList"
import { RecordDetail } from "../../../components/RecordDetail"
import { Facts, Field, SelectField } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useRecord } from "../../../hooks/useRecord"
import { useRemote } from "../../../hooks/useRemote"
import { useLookups } from "../../../hooks/useLookups"
import { useListFilters } from "../../../hooks/useListFilters"

import type { DryingLot } from "../types/DryingLot"
import { dryingLotApi } from "../services/DryingLotApi"
import { DryingLotForm } from "../components/DryingLotForm"
import { enumLabels } from "../../../services/labels"
import { DryingActions } from "../components/DryingActions"
export function DryingLotPage({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
if (route.id === "novo") return <DryingLotForm garden={route.garden} saved={record => { saved("Registro salvo. Recarregando dados confirmados."); goToSame(route, record.id, route.search) }} cancel={() => goToSame(route, '', route.search)} />
if (route.id) return <DryingLotDetail route={route} saved={saved} />
return <DryingLotList route={route} />
}
function DryingLotList({ route }: { route: AppRoute }) {
const filters = useListFilters(route, ['from', 'to', 'validity', 'status'], true)
const options = useLookups(route.garden, [])
const params = { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, status: filters.values.status || undefined }
const queryKey = JSON.stringify(params)
const load = useCallback((signal: AbortSignal) => dryingLotApi.list(route.garden, { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, status: filters.values.status || undefined }, signal), [filters.page, filters.values.from, filters.values.to, filters.values.validity, filters.values.status, route.garden])
const result = useRemote(queryKey + "/" + filters.revision, load)
return <RecordList title="Lotes de secagem" hint="Período da data do registro. Consulte a origem, corrija com motivo e preserve o histórico." create={() => goToSame(route, "novo", route.search)} apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} data={result.data} page={filters.changePage} filters={<>
<Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change("from", value)} error={filters.error} />
<Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change("to", value)} error={filters.error} />
<SelectField name="validity" label="Validade" value={filters.draft.validity} change={value => filters.change("validity", value)} options={[{ id: 'Valid', label: enumLabels['Valid'] }, { id: 'Invalid', label: enumLabels['Invalid'] }]} />
<SelectField name="status" label="Situação" value={filters.draft.status} change={value => filters.change("status", value)} options={[{ id: 'InProgress', label: enumLabels['InProgress'] }, { id: 'Completed', label: enumLabels['Completed'] }]} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}>
{result.data?.items.map(item => <RecordRow key={item.id} href={samePage(route, item.id, route.search)} title={`Secagem de ${item.startedOn.split("-").reverse().join("/")}`} description={item.method ?? "Método não informado"} meta={<span className="text-xs text-horta-muted">{enumLabels[item.status] ?? item.status}</span>} status={item.validity === "Invalid" ? enumLabels[item.validity] : undefined} statusTone="invalid" />)}
</RecordList>
}
function DryingLotDetail({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
const get = useCallback((signal: AbortSignal) => dryingLotApi.get(route.garden, route.id, signal), [route.id, route.garden])
const audit = useCallback((page: number, signal: AbortSignal) => dryingLotApi.history(route.garden, route.id, page, signal), [route.id, route.garden])
const state = useRecord<DryingLot>(route.id, get, audit, (version, reason) => dryingLotApi.deactivate(route.garden, route.id, version, reason), () => saved("Alteração confirmada."))
const record = state.detail.data
const options = useLookups(route.garden, ['harvest', 'participant'])
if (route.action === 'editar' && record) return <DryingLotForm garden={route.garden} original={record} saved={record => { saved("Registro salvo. Dados e indicadores serão recarregados."); const query = new URLSearchParams(route.search); goToSame(route, record.id, query); state.refresh() }} cancel={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query) }} />
const lotName = record ? record.code || `Secagem de ${formatDate(record.startedOn)}` : 'Lote de secagem'
// Resumo no cabeçalho; os dados completos ficam numa grade única, sem identificadores técnicos.
const description = record && [enumLabels[record.status], `início em ${formatDate(record.startedOn)}`, record.method].filter(Boolean).join(' · ')
const facts = record && <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[
  { label: 'Situação', value: enumLabels[record.status] },
  { label: 'Método', value: record.method || 'Não informado' },
  { label: 'Data de início', value: formatDate(record.startedOn) },
  { label: 'Data de encerramento', value: record.endedOn ? formatDate(record.endedOn) : 'Em andamento' },
  { label: 'Massa seca total', value: record.totalDryMassKg === null ? 'Não pesada' : `${formatNumber(record.totalDryMassKg)} kg` },
  { label: 'Responsável pelo trabalho', value: record.responsibleParticipantId ? <a className="underline" href={linkTo(route.garden, 'participantes', record.responsibleParticipantId)}>{choiceLabel(options.data?.participant, record.responsibleParticipantId) ?? 'Participante'}</a> : 'Não informado' },
  { label: 'Incluído em', value: formatInstant(record.recordedAt) },
]} /></>
const origins = record && <div className="cycle-records">
  <DryingActions garden={route.garden} record={record} changed={() => saved("Alteração confirmada. Dados e indicadores recarregados.")} />
  <section className="cycle-record-group" aria-label="Colheitas de origem">
    <header><h3>Colheitas de origem <span>({record.items.length})</span></h3></header>
    {record.items.length === 0 ? <p className="cycle-record-empty">Nenhuma colheita de origem.</p> : <ul>
      {record.items.map((item, index) => <li key={`${item.harvestId}/${index}`}><a href={linkTo(route.garden, 'producao/colheitas', item.harvestId)}>
        <strong>{formatNumber(item.allocatedMassKg)} kg destinados</strong>
        <span>{choiceLabel(options.data?.harvest, item.harvestId) ?? 'Colheita'}</span>
      </a></li>)}
    </ul>}
  </section>
</div>
return <RecordDetail state={state} eyebrow="Lote de secagem" title={lotName} description={description} version={record?.version ?? 0} active={record?.validity === "Valid"} catalog={false}
  back={() => { const query = new URLSearchParams(route.search); goToSame(route, '', query) }} edit={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query, 'editar') }}
  sections={origins ? [{ id: 'origens', label: 'Origens', content: <div className="panel">{origins}</div> }] : []}
  items={facts ? [{ label: 'Dados do lote', value: facts }] : []}
  attachments={record && <EvidenceSection garden={route.garden} targetType="DryingLot" targetId={record.id} active={record.validity === "Valid"} />} />
}
