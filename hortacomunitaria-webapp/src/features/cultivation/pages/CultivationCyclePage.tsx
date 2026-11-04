import { formatNumber } from "../../irrigation/formValues"
import { formatDate } from "../../../services/dates"
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
import type { CultivationCycle } from "../types/CultivationCycle"
import { cultivationCycleApi } from "../services/CultivationCycleApi"
import { CultivationCycleForm, CultivationCycleReview } from "../components/CultivationCycleForm"
import { enumLabels } from "../../../services/labels"
import { CycleActions } from "../components/CycleActions"
export function CultivationCyclePage({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
if (route.id === "novo") return <CultivationCycleForm garden={route.garden} saved={record => { saved("Registro salvo. Recarregando dados confirmados."); navigate(route.garden, route.module, route.resource, record.id, route.search) }} cancel={() => navigate(route.garden, route.module, route.resource, "", route.search)} />
if (route.id) return <CultivationCycleDetail route={route} saved={saved} />
return <CultivationCycleList route={route} />
}
function CultivationCycleList({ route }: { route: RecordRoute }) {
const filters = useListFilters(route, ['from', 'to', 'validity', 'bedId', 'speciesId', 'status'], true)
const options = useLookups(route.garden, ['bed', 'species'])
const params = { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, bedId: filters.values.bedId || undefined, speciesId: filters.values.speciesId || undefined, status: filters.values.status || undefined }
const queryKey = JSON.stringify(params)
const load = useCallback((signal: AbortSignal) => cultivationCycleApi.list(route.garden, { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, bedId: filters.values.bedId || undefined, speciesId: filters.values.speciesId || undefined, status: filters.values.status || undefined }, signal), [filters.page, filters.values.from, filters.values.to, filters.values.validity, filters.values.bedId, filters.values.speciesId, filters.values.status, route.garden])
const result = useRemote(queryKey + "/" + filters.revision, load)
return <RecordList title="Ciclos de cultivo" hint="Período da data do registro. Consulte a origem, corrija com motivo e preserve o histórico." create={() => navigate(route.garden, route.module, route.resource, "novo", route.search)} apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} data={result.data} page={filters.changePage} filters={<>
<Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change("from", value)} error={filters.error} />
<Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change("to", value)} error={filters.error} />
<SelectField name="validity" label="Validade" value={filters.draft.validity} change={value => filters.change("validity", value)} options={[{ id: 'Valid', label: enumLabels['Valid'] }, { id: 'Invalid', label: enumLabels['Invalid'] }]} />
<SelectField name="bedId" label="Canteiro" value={filters.draft.bedId} change={value => filters.change("bedId", value)} options={options.data?.bed ?? []} />
<SelectField name="speciesId" label="Espécie" value={filters.draft.speciesId} change={value => filters.change("speciesId", value)} options={options.data?.species ?? []} />
<SelectField name="status" label="Situação" value={filters.draft.status} change={value => filters.change("status", value)} options={[{ id: 'InProgress', label: enumLabels['InProgress'] }, { id: 'Closed', label: enumLabels['Closed'] }]} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}>
{result.data?.items.map(item => <li className="py-4" key={item.id}><a className="font-semibold underline decoration-horta-line underline-offset-4" href={routeHref(route.garden, route.module, route.resource, item.id, route.search)}>{formatDate(item.startedOn)}</a><span className="ml-3 text-sm text-horta-muted">{enumLabels[item.validity]} · v{item.version}</span><small className="record-description">{enumLabels[item.status]} · Área ocupada: {formatNumber(item.occupiedAreaM2)} m²</small></li>)}
</RecordList>
}
function CultivationCycleDetail({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
const get = useCallback((signal: AbortSignal) => cultivationCycleApi.get(route.garden, route.id, signal), [route.id, route.garden])
const audit = useCallback((page: number, signal: AbortSignal) => cultivationCycleApi.history(route.garden, route.id, page, signal), [route.id, route.garden])
const state = useRecord<CultivationCycle>(route.id, get, audit, (version, reason) => cultivationCycleApi.deactivate(route.garden, route.id, version, reason), () => saved("Alteração confirmada."))
const record = state.detail.data
if (route.search.get("edit") === "1" && record) return <CultivationCycleForm garden={route.garden} original={record} saved={record => { saved("Registro salvo. Dados e indicadores serão recarregados."); const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, record.id, query); state.refresh() }} cancel={() => { const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, route.id, query) }} />
return <RecordDetail state={state} title="Ciclo de cultivo" version={record?.version ?? 0} active={record?.validity === "Valid"} catalog={false} back={() => { const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, "", query) }} edit={() => { const query = new URLSearchParams(route.search); query.set("edit", "1"); navigate(route.garden, route.module, route.resource, route.id, query) }} items={record ? [{ label: "Valores do registro", value: <CultivationCycleReview record={record} /> }, { label: "Incluído em", value: formatInstant(record.recordedAt) }, { label: "Autor técnico (demonstração)", value: record.recordedByUserId }, { label: "Área de referência preservada (m²)", value: record.referenceAreaM2 }, { label: "Configuração de origem", value: record.bedConfigurationId }, { label: "Situação", value: enumLabels[record.status] }] : []}>{record && <><CycleActions route={route} record={record} changed={state.refresh} /></>}</RecordDetail>
}
