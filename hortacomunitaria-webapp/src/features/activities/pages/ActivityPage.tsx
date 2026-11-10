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
import { navigate, routeHref } from "../../../hooks/useRoute"
import type { RecordRoute } from "../../../types/records"
import type { Activity } from "../types/Activity"
import { activityApi } from "../services/ActivityApi"
import { ActivityForm, ActivityReview } from "../components/ActivityForm"
import { enumLabels } from "../../../services/labels"
export function ActivityPage({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
if (route.id === "novo") return <ActivityForm garden={route.garden} saved={record => { saved("Registro salvo. Recarregando dados confirmados."); navigate(route.garden, route.module, route.resource, record.id, route.search) }} cancel={() => navigate(route.garden, route.module, route.resource, "", route.search)} />
if (route.id) return <ActivityDetail route={route} saved={saved} />
return <ActivityList route={route} />
}
function ActivityList({ route }: { route: RecordRoute }) {
const filters = useListFilters(route, ['from', 'to', 'validity', 'bedId', 'sectorId', 'cycleId', 'occurrenceId', 'category', 'isSocial', 'isEducational', 'institutionId'], true)
const options = useLookups(route.garden, ['bed', 'cultivationCycle', 'institution', 'irrigationSector', 'occurrence'])
const params = { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, bedId: filters.values.bedId || undefined, sectorId: filters.values.sectorId || undefined, cycleId: filters.values.cycleId || undefined, occurrenceId: filters.values.occurrenceId || undefined, category: filters.values.category || undefined, isSocial: filters.values.isSocial ? filters.values.isSocial === "true" : undefined, isEducational: filters.values.isEducational ? filters.values.isEducational === "true" : undefined, institutionId: filters.values.institutionId || undefined }
const queryKey = JSON.stringify(params)
const load = useCallback((signal: AbortSignal) => activityApi.list(route.garden, { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, bedId: filters.values.bedId || undefined, sectorId: filters.values.sectorId || undefined, cycleId: filters.values.cycleId || undefined, occurrenceId: filters.values.occurrenceId || undefined, category: filters.values.category || undefined, isSocial: filters.values.isSocial ? filters.values.isSocial === "true" : undefined, isEducational: filters.values.isEducational ? filters.values.isEducational === "true" : undefined, institutionId: filters.values.institutionId || undefined }, signal), [filters.page, filters.values.from, filters.values.to, filters.values.validity, filters.values.bedId, filters.values.sectorId, filters.values.cycleId, filters.values.occurrenceId, filters.values.category, filters.values.isSocial, filters.values.isEducational, filters.values.institutionId, route.garden])
const result = useRemote(queryKey + "/" + filters.revision, load)
return <RecordList title="Atividades" hint="Período da data do registro. Consulte a origem, corrija com motivo e preserve o histórico." create={() => navigate(route.garden, route.module, route.resource, "novo", route.search)} apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} data={result.data} page={filters.changePage} filters={<>
<Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change("from", value)} error={filters.error} />
<Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change("to", value)} error={filters.error} />
<SelectField name="validity" label="Validade" value={filters.draft.validity} change={value => filters.change("validity", value)} options={[{ id: 'Valid', label: enumLabels['Valid'] }, { id: 'Invalid', label: enumLabels['Invalid'] }]} />
<SelectField name="bedId" label="Canteiro" value={filters.draft.bedId} change={value => filters.change("bedId", value)} options={options.data?.bed ?? []} />
<SelectField name="sectorId" label="Setor" value={filters.draft.sectorId} change={value => filters.change("sectorId", value)} options={options.data?.irrigationSector ?? []} />
<SelectField name="cycleId" label="Ciclo" value={filters.draft.cycleId} change={value => filters.change("cycleId", value)} options={options.data?.cultivationCycle ?? []} />
<SelectField name="occurrenceId" label="Ocorrência vinculada" value={filters.draft.occurrenceId} change={value => filters.change("occurrenceId", value)} options={options.data?.occurrence ?? []} />
<Field name="category" label="Categoria" type="text" value={filters.draft.category} change={value => filters.change("category", value)} />
<SelectField name="isSocial" label="Social" value={filters.draft.isSocial} change={value => filters.change("isSocial", value)} options={[{ id: 'true', label: 'Sim' }, { id: 'false', label: 'Não' }]} />
<SelectField name="isEducational" label="Educacional" value={filters.draft.isEducational} change={value => filters.change("isEducational", value)} options={[{ id: 'true', label: 'Sim' }, { id: 'false', label: 'Não' }]} />
<SelectField name="institutionId" label="Instituição" value={filters.draft.institutionId} change={value => filters.change("institutionId", value)} options={options.data?.institution ?? []} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}>
{result.data?.items.map(item => <RecordRow key={item.id} href={routeHref(route.garden, route.module, route.resource, item.id, route.search)} title={item.title} description={`${item.category} · ${item.performedOn.split("-").reverse().join("/")}`} status={item.validity === "Invalid" ? enumLabels[item.validity] : undefined} statusTone="invalid" />)}
</RecordList>
}
function ActivityDetail({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
const get = useCallback((signal: AbortSignal) => activityApi.get(route.garden, route.id, signal), [route.id, route.garden])
const audit = useCallback((page: number, signal: AbortSignal) => activityApi.history(route.garden, route.id, page, signal), [route.id, route.garden])
const state = useRecord<Activity>(route.id, get, audit, (version, reason) => activityApi.deactivate(route.garden, route.id, version, reason), () => saved("Alteração confirmada."))
const record = state.detail.data
if (route.search.get("edit") === "1" && record) return <ActivityForm garden={route.garden} original={record} saved={record => { saved("Registro salvo. Dados e indicadores serão recarregados."); const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, record.id, query); state.refresh() }} cancel={() => { const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, route.id, query) }} />
return <RecordDetail attachments={record && <EvidenceSection garden={route.garden} targetType="Activity" targetId={record.id} active={record.validity === "Valid"} />} state={state} title="Atividade" version={record?.version ?? 0} active={record?.validity === "Valid"} catalog={false} back={() => { const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, "", query) }} edit={() => { const query = new URLSearchParams(route.search); query.set("edit", "1"); navigate(route.garden, route.module, route.resource, route.id, query) }} items={record ? [{ label: "Valores do registro", value: <ActivityReview record={record} /> }, { label: "Incluído em", value: formatInstant(record.recordedAt) }, { label: "Autor técnico (demonstração)", value: record.recordedByUserId }] : []}>{record && <></>}</RecordDetail>
}
