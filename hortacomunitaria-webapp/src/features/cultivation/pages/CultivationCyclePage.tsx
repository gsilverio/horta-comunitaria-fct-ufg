import type { AppRoute } from '../../../routes/routes'
import { goToSame } from '../../../routes/navigation'
import { linkTo, samePage } from '../../../routes/routes'
import { EvidenceSection } from "../../evidence/pages/EvidenceSection"
import { RecordRow } from '../../../components/RecordRow'
import { choiceLabel } from '../../../services/lookups'
import { formatNumber } from "../../irrigation/formValues"
import { formatDate } from "../../../services/dates"
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

import type { CultivationCycle } from "../types/CultivationCycle"
import { cultivationCycleApi } from "../services/CultivationCycleApi"
import { CultivationCycleForm } from "../components/CultivationCycleForm"
import { enumLabels } from "../../../services/labels"
import { CycleActions } from "../components/CycleActions"
export function CultivationCyclePage({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
if (route.id === "novo") return <CultivationCycleForm garden={route.garden} saved={record => { saved("Registro salvo. Recarregando dados confirmados."); goToSame(route, record.id, route.search) }} cancel={() => goToSame(route, '', route.search)} />
if (route.id) return <CultivationCycleDetail route={route} saved={saved} />
return <CultivationCycleList route={route} />
}
function CultivationCycleList({ route }: { route: AppRoute }) {
const filters = useListFilters(route, ['from', 'to', 'validity', 'bedId', 'speciesId', 'status'], true)
const options = useLookups(route.garden, ['bed', 'species'])
const params = { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, bedId: filters.values.bedId || undefined, speciesId: filters.values.speciesId || undefined, status: filters.values.status || undefined }
const queryKey = JSON.stringify(params)
const load = useCallback((signal: AbortSignal) => cultivationCycleApi.list(route.garden, { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, bedId: filters.values.bedId || undefined, speciesId: filters.values.speciesId || undefined, status: filters.values.status || undefined }, signal), [filters.page, filters.values.from, filters.values.to, filters.values.validity, filters.values.bedId, filters.values.speciesId, filters.values.status, route.garden])
const result = useRemote(queryKey + "/" + filters.revision, load)
return <RecordList title="Ciclos de cultivo" hint="Período da data do registro. Consulte a origem, corrija com motivo e preserve o histórico." create={() => goToSame(route, "novo", route.search)} apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} data={result.data} page={filters.changePage} filters={<>
<Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change("from", value)} error={filters.error} />
<Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change("to", value)} error={filters.error} />
<SelectField name="validity" label="Validade" value={filters.draft.validity} change={value => filters.change("validity", value)} options={[{ id: 'Valid', label: enumLabels['Valid'] }, { id: 'Invalid', label: enumLabels['Invalid'] }]} />
<SelectField name="bedId" label="Canteiro" value={filters.draft.bedId} change={value => filters.change("bedId", value)} options={options.data?.bed ?? []} />
<SelectField name="speciesId" label="Espécie" value={filters.draft.speciesId} change={value => filters.change("speciesId", value)} options={options.data?.species ?? []} />
<SelectField name="status" label="Situação" value={filters.draft.status} change={value => filters.change("status", value)} options={[{ id: 'InProgress', label: enumLabels['InProgress'] }, { id: 'Closed', label: enumLabels['Closed'] }]} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}>
{result.data?.items.map(item => <RecordRow key={item.id} href={samePage(route, item.id, route.search)} title={`${choiceLabel(options.data?.species, item.speciesId) ?? "Espécie"} · ${choiceLabel(options.data?.bed, item.bedId) ?? "Canteiro"}`} description={`Início em ${formatDate(item.startedOn)} · ${formatNumber(item.occupiedAreaM2)} m²`} meta={<span className="text-xs text-horta-muted">{enumLabels[item.status]}</span>} status={item.validity === "Invalid" ? enumLabels[item.validity] : undefined} statusTone="invalid" />)}
</RecordList>
}
function CultivationCycleDetail({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
const get = useCallback((signal: AbortSignal) => cultivationCycleApi.get(route.garden, route.id, signal), [route.id, route.garden])
const audit = useCallback((page: number, signal: AbortSignal) => cultivationCycleApi.history(route.garden, route.id, page, signal), [route.id, route.garden])
const state = useRecord<CultivationCycle>(route.id, get, audit, (version, reason) => cultivationCycleApi.deactivate(route.garden, route.id, version, reason), () => saved("Alteração confirmada."))
const record = state.detail.data
const options = useLookups(route.garden, ['bed', 'species', 'cultivar', 'participant'])
if (route.action === 'editar' && record) return <CultivationCycleForm garden={route.garden} original={record} saved={record => { saved("Registro salvo. Dados e indicadores serão recarregados."); const query = new URLSearchParams(route.search); goToSame(route, record.id, query); state.refresh() }} cancel={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query) }} />
const label = (kind: 'bed' | 'species' | 'cultivar' | 'participant', id: string | null) => id ? options.data?.[kind]?.find(item => item.id === id)?.label : undefined
const speciesName = record ? label('species', record.speciesId) ?? 'Espécie' : 'Ciclo de cultivo'
const bedName = record ? label('bed', record.bedId) ?? 'Canteiro' : ''
// Resumo no cabeçalho; os dados completos ficam numa grade única, sem identificadores técnicos.
const description = record && `${bedName} · ${enumLabels[record.status]}${record.status === 'Closed' && record.endedOn ? ` em ${formatDate(record.endedOn)}` : ` desde ${formatDate(record.startedOn)}`} · ${formatNumber(record.occupiedAreaM2)} m²`
const facts = record && <Facts items={[
  { label: 'Canteiro', value: <a className="underline" href={linkTo(route.garden, 'horta/canteiros', record.bedId)}>{bedName}</a> },
  { label: 'Espécie', value: <a className="underline" href={linkTo(route.garden, 'catalogo/especies', record.speciesId)}>{speciesName}</a> },
  { label: 'Cultivar', value: record.cultivarId ? <a className="underline" href={linkTo(route.garden, 'catalogo/cultivares', record.cultivarId, new URLSearchParams({ speciesId: record.speciesId }))}>{label('cultivar', record.cultivarId) ?? 'Cultivar'}</a> : 'Não informada' },
  { label: 'Início', value: formatDate(record.startedOn) },
  { label: record.status === 'Closed' ? 'Encerramento' : 'Situação', value: record.status === 'Closed' && record.endedOn ? formatDate(record.endedOn) : enumLabels[record.status] },
  { label: 'Área ocupada', value: `${formatNumber(record.occupiedAreaM2)} m²` },
  { label: 'Área do canteiro no início', value: `${formatNumber(record.referenceAreaM2)} m²` },
  { label: 'Responsável pelo trabalho', value: label('participant', record.responsibleParticipantId) ?? 'Não informado' },
  { label: 'Incluído em', value: formatInstant(record.recordedAt) },
]} />
return <RecordDetail state={state} eyebrow="Ciclo de cultivo" title={speciesName} description={description} version={record?.version ?? 0} active={record?.validity === "Valid"} catalog={false}
  back={() => { const query = new URLSearchParams(route.search); goToSame(route, '', query) }} edit={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query, 'editar') }}
  sections={record ? [{ id: 'registros', label: 'Registros', content: <div className="panel"><CycleActions route={route} record={record} changed={() => saved("Alteração confirmada. Dados e indicadores recarregados.")} /></div> }] : []}
  items={facts ? [{ label: 'Dados do ciclo', value: facts }] : []}
  attachments={record && <EvidenceSection garden={route.garden} targetType="CultivationCycle" targetId={record.id} active={record.validity === "Valid"} />} />
}
