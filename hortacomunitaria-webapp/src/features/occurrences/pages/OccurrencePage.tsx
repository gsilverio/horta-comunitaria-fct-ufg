import type { AppRoute } from '../../../routes/routes'
import { goToSame } from '../../../routes/navigation'
import { samePage } from '../../../routes/routes'
import { EvidenceSection } from "../../evidence/pages/EvidenceSection"
import { RecordRow } from '../../../components/RecordRow'
import { formatInstant } from "../../irrigation/formValues"
import { useCallback } from "react"
import { RecordList } from "../../../components/RecordList"
import { RecordDetail } from "../../../components/RecordDetail"
import { Field, SelectField } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useRecord } from "../../../hooks/useRecord"
import { useRemote } from "../../../hooks/useRemote"
import { useLookups } from "../../../hooks/useLookups"
import { useListFilters } from "../../../hooks/useListFilters"

import type { Occurrence } from "../types/Occurrence"
import { occurrenceApi } from "../services/OccurrenceApi"
import { OccurrenceForm, OccurrenceReview } from "../components/OccurrenceForm"
import { enumLabels } from "../../../services/labels"
import { OccurrenceActions } from "../components/OccurrenceActions"
export function OccurrencePage({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
if (route.id === "novo") return <OccurrenceForm garden={route.garden} saved={record => { saved("Registro salvo. Recarregando dados confirmados."); goToSame(route, record.id, route.search) }} cancel={() => goToSame(route, '', route.search)} />
if (route.id) return <OccurrenceDetail route={route} saved={saved} />
return <OccurrenceList route={route} />
}
function OccurrenceList({ route }: { route: AppRoute }) {
const filters = useListFilters(route, ['from', 'to', 'validity', 'category', 'status', 'bedId', 'sectorId', 'cycleId', 'irrigationId'], true)
const options = useLookups(route.garden, ['bed', 'cultivationCycle', 'irrigation', 'irrigationSector'])
const params = { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, category: filters.values.category || undefined, status: filters.values.status || undefined, bedId: filters.values.bedId || undefined, sectorId: filters.values.sectorId || undefined, cycleId: filters.values.cycleId || undefined, irrigationId: filters.values.irrigationId || undefined }
const queryKey = JSON.stringify(params)
const load = useCallback((signal: AbortSignal) => occurrenceApi.list(route.garden, { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, category: filters.values.category || undefined, status: filters.values.status || undefined, bedId: filters.values.bedId || undefined, sectorId: filters.values.sectorId || undefined, cycleId: filters.values.cycleId || undefined, irrigationId: filters.values.irrigationId || undefined }, signal), [filters.page, filters.values.from, filters.values.to, filters.values.validity, filters.values.category, filters.values.status, filters.values.bedId, filters.values.sectorId, filters.values.cycleId, filters.values.irrigationId, route.garden])
const result = useRemote(queryKey + "/" + filters.revision, load)
return <RecordList title="Ocorrências" hint="Período da data do registro. Consulte a origem, corrija com motivo e preserve o histórico." create={() => goToSame(route, "novo", route.search)} apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} data={result.data} page={filters.changePage} filters={<>
<Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change("from", value)} error={filters.error} />
<Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change("to", value)} error={filters.error} />
<SelectField name="validity" label="Validade" value={filters.draft.validity} change={value => filters.change("validity", value)} options={[{ id: 'Valid', label: enumLabels['Valid'] }, { id: 'Invalid', label: enumLabels['Invalid'] }]} />
<Field name="category" label="Categoria" type="text" value={filters.draft.category} change={value => filters.change("category", value)} />
<SelectField name="status" label="Situação" value={filters.draft.status} change={value => filters.change("status", value)} options={[{ id: 'Open', label: enumLabels['Open'] }, { id: 'InProgress', label: enumLabels['InProgress'] }, { id: 'Resolved', label: enumLabels['Resolved'] }]} />
<SelectField name="bedId" label="Canteiro" value={filters.draft.bedId} change={value => filters.change("bedId", value)} options={options.data?.bed ?? []} />
<SelectField name="sectorId" label="Setor" value={filters.draft.sectorId} change={value => filters.change("sectorId", value)} options={options.data?.irrigationSector ?? []} />
<SelectField name="cycleId" label="Ciclo" value={filters.draft.cycleId} change={value => filters.change("cycleId", value)} options={options.data?.cultivationCycle ?? []} />
<SelectField name="irrigationId" label="Irrigação" value={filters.draft.irrigationId} change={value => filters.change("irrigationId", value)} options={options.data?.irrigation ?? []} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}>
{result.data?.items.map(item => <RecordRow key={item.id} href={samePage(route, item.id, route.search)} title={item.description} description={`${item.category} · ${formatInstant(item.openedAt)}`} meta={<span className="text-xs text-horta-muted">{enumLabels[item.status] ?? item.status}</span>} status={item.validity === "Invalid" ? enumLabels[item.validity] : undefined} statusTone="invalid" />)}
</RecordList>
}
function OccurrenceDetail({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
const get = useCallback((signal: AbortSignal) => occurrenceApi.get(route.garden, route.id, signal), [route.id, route.garden])
const audit = useCallback((page: number, signal: AbortSignal) => occurrenceApi.history(route.garden, route.id, page, signal), [route.id, route.garden])
const state = useRecord<Occurrence>(route.id, get, audit, (version, reason) => occurrenceApi.deactivate(route.garden, route.id, version, reason), () => saved("Alteração confirmada."))
const record = state.detail.data
if (route.action === 'editar' && record) return <OccurrenceForm garden={route.garden} original={record} saved={record => { saved("Registro salvo. Dados e indicadores serão recarregados."); const query = new URLSearchParams(route.search); goToSame(route, record.id, query); state.refresh() }} cancel={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query) }} />
return <RecordDetail attachments={record && <EvidenceSection garden={route.garden} targetType="Occurrence" targetId={record.id} active={record.validity === "Valid"} />} state={state} title="Ocorrência" version={record?.version ?? 0} active={record?.validity === "Valid"} catalog={false} back={() => { const query = new URLSearchParams(route.search); goToSame(route, '', query) }} edit={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query, 'editar') }} items={record ? [{ label: "Valores do registro", value: <OccurrenceReview record={record} /> }, { label: "Incluído em", value: formatInstant(record.recordedAt) }, { label: "Autor do registro (identificador)", value: record.recordedByUserId }] : []}>{record && <><OccurrenceActions route={route} record={record} changed={state.refresh} /></>}</RecordDetail>
}
