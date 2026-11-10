import { RecordRow } from '../../../components/RecordRow'
import { useCallback } from "react"
import { RecordList } from "../../../components/RecordList"
import { RecordDetail } from "../../../components/RecordDetail"
import { SelectField } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useRecord } from "../../../hooks/useRecord"
import { useRemote } from "../../../hooks/useRemote"
import { useLookups } from "../../../hooks/useLookups"
import { useListFilters } from "../../../hooks/useListFilters"
import { navigate, routeHref } from "../../../hooks/useRoute"
import type { RecordRoute } from "../../../types/records"
import type { SamplingPoint } from "../types/SamplingPoint"
import { samplingPointApi } from "../services/SamplingPointApi"
import { SamplingPointForm, SamplingPointReview } from "../components/SamplingPointForm"
export function SamplingPointPage({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
if (route.id === "novo") return <SamplingPointForm garden={route.garden} saved={record => { saved("Registro salvo. Recarregando dados confirmados."); navigate(route.garden, route.module, route.resource, record.id, route.search) }} cancel={() => navigate(route.garden, route.module, route.resource, "", route.search)} />
if (route.id) return <SamplingPointDetail route={route} saved={saved} />
return <SamplingPointList route={route} />
}
function SamplingPointList({ route }: { route: RecordRoute }) {
const filters = useListFilters(route, ['isActive'], false)
const options = useLookups(route.garden, [])
const params = { page: filters.page, pageSize: 10, isActive: filters.values.isActive ? filters.values.isActive === "true" : undefined }
const queryKey = JSON.stringify(params)
const load = useCallback((signal: AbortSignal) => samplingPointApi.list(route.garden, { page: filters.page, pageSize: 10, isActive: filters.values.isActive ? filters.values.isActive === "true" : undefined }, signal), [filters.page, filters.values.isActive, route.garden])
const result = useRemote(queryKey + "/" + filters.revision, load)
return <RecordList title="Pontos de coleta" hint="Cadastros de apoio com identificação, versão e histórico." create={() => navigate(route.garden, route.module, route.resource, "novo", route.search)} apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} data={result.data} page={filters.changePage} filters={<>
<SelectField name="isActive" label="Situação cadastral" value={filters.draft.isActive} change={value => filters.change("isActive", value)} options={[{ id: 'true', label: 'Ativo' }, { id: 'false', label: 'Inativo' }]} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}>
{result.data?.items.map(item => <RecordRow key={item.id} href={routeHref(route.garden, route.module, route.resource, item.id, route.search)} title={item.description} description={item.location ?? undefined} status={item.isActive ? undefined : "Inativo"} statusTone="invalid" />)}
</RecordList>
}
function SamplingPointDetail({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
const get = useCallback((signal: AbortSignal) => samplingPointApi.get(route.garden, route.id, signal), [route.id, route.garden])
const audit = useCallback((page: number, signal: AbortSignal) => samplingPointApi.history(route.garden, route.id, page, signal), [route.id, route.garden])
const state = useRecord<SamplingPoint>(route.id, get, audit, (version, reason) => samplingPointApi.deactivate(route.garden, route.id, version, reason), () => saved("Alteração confirmada."))
const record = state.detail.data
if (route.search.get("edit") === "1" && record) return <SamplingPointForm garden={route.garden} original={record} saved={record => { saved("Registro salvo. Dados e indicadores serão recarregados."); const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, record.id, query); state.refresh() }} cancel={() => { const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, route.id, query) }} />
return <RecordDetail state={state} title="Ponto de coleta" version={record?.version ?? 0} active={record?.isActive === true} catalog={true} back={() => { const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, "", query) }} edit={() => { const query = new URLSearchParams(route.search); query.set("edit", "1"); navigate(route.garden, route.module, route.resource, route.id, query) }} items={record ? [{ label: "Valores do registro", value: <SamplingPointReview record={record} /> }] : []}>{record && <></>}</RecordDetail>
}
