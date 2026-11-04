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
import type { DryingLot } from "../types/DryingLot"
import { dryingLotApi } from "../services/DryingLotApi"
import { DryingLotForm, DryingLotReview } from "../components/DryingLotForm"
import { enumLabels } from "../../../services/labels"
import { DryingActions } from "../components/DryingActions"
export function DryingLotPage({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
if (route.id === "novo") return <DryingLotForm garden={route.garden} saved={record => { saved("Registro salvo. Recarregando dados confirmados."); navigate(route.garden, route.module, route.resource, record.id, route.search) }} cancel={() => navigate(route.garden, route.module, route.resource, "", route.search)} />
if (route.id) return <DryingLotDetail route={route} saved={saved} />
return <DryingLotList route={route} />
}
function DryingLotList({ route }: { route: RecordRoute }) {
const filters = useListFilters(route, ['from', 'to', 'validity', 'status'], true)
const options = useLookups(route.garden, [])
const params = { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, status: filters.values.status || undefined }
const queryKey = JSON.stringify(params)
const load = useCallback((signal: AbortSignal) => dryingLotApi.list(route.garden, { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, status: filters.values.status || undefined }, signal), [filters.page, filters.values.from, filters.values.to, filters.values.validity, filters.values.status, route.garden])
const result = useRemote(queryKey + "/" + filters.revision, load)
return <RecordList title="Lotes de secagem" hint="Período da data do registro. Consulte a origem, corrija com motivo e preserve o histórico." create={() => navigate(route.garden, route.module, route.resource, "novo", route.search)} apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} data={result.data} page={filters.changePage} filters={<>
<Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change("from", value)} error={filters.error} />
<Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change("to", value)} error={filters.error} />
<SelectField name="validity" label="Validade" value={filters.draft.validity} change={value => filters.change("validity", value)} options={[{ id: 'Valid', label: enumLabels['Valid'] }, { id: 'Invalid', label: enumLabels['Invalid'] }]} />
<SelectField name="status" label="Situação" value={filters.draft.status} change={value => filters.change("status", value)} options={[{ id: 'InProgress', label: enumLabels['InProgress'] }, { id: 'Completed', label: enumLabels['Completed'] }]} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}>
{result.data?.items.map(item => <li className="py-4" key={item.id}><a className="font-semibold underline decoration-horta-line underline-offset-4" href={routeHref(route.garden, route.module, route.resource, item.id, route.search)}>Secagem de {item.startedOn.split("-").reverse().join("/")} · {item.method ?? "Método não informado"}</a><span className="ml-3 text-sm text-horta-muted">{enumLabels[item.validity]} · v{item.version}</span></li>)}
</RecordList>
}
function DryingLotDetail({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
const get = useCallback((signal: AbortSignal) => dryingLotApi.get(route.garden, route.id, signal), [route.id, route.garden])
const audit = useCallback((page: number, signal: AbortSignal) => dryingLotApi.history(route.garden, route.id, page, signal), [route.id, route.garden])
const state = useRecord<DryingLot>(route.id, get, audit, (version, reason) => dryingLotApi.deactivate(route.garden, route.id, version, reason), () => saved("Alteração confirmada."))
const record = state.detail.data
if (route.search.get("edit") === "1" && record) return <DryingLotForm garden={route.garden} original={record} saved={record => { saved("Registro salvo. Dados e indicadores serão recarregados."); const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, record.id, query); state.refresh() }} cancel={() => { const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, route.id, query) }} />
return <RecordDetail state={state} title="Lote de secagem" version={record?.version ?? 0} active={record?.validity === "Valid"} catalog={false} back={() => { const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, "", query) }} edit={() => { const query = new URLSearchParams(route.search); query.set("edit", "1"); navigate(route.garden, route.module, route.resource, route.id, query) }} items={record ? [{ label: "Valores do registro", value: <DryingLotReview record={record} /> }, { label: "Incluído em", value: formatInstant(record.recordedAt) }, { label: "Autor técnico (demonstração)", value: record.recordedByUserId }, { label: "Situação", value: enumLabels[record.status] }] : []}>{record && <><DryingActions garden={route.garden} record={record} changed={state.refresh} /></>}</RecordDetail>
}
