import type { AppRoute } from '../../../routes/routes'
import { goToSame } from '../../../routes/navigation'
import { samePage } from '../../../routes/routes'
import { RecordRow } from '../../../components/RecordRow'
import { enumLabels } from '../../../services/labels'
import { useCallback } from "react"
import { RecordList } from "../../../components/RecordList"
import { RecordDetail } from "../../../components/RecordDetail"
import { SelectField } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useRecord } from "../../../hooks/useRecord"
import { useRemote } from "../../../hooks/useRemote"
import { useLookups } from "../../../hooks/useLookups"
import { useListFilters } from "../../../hooks/useListFilters"

import type { PropagationLot } from "../types/PropagationLot"
import { propagationLotApi } from "../services/PropagationLotApi"
import { PropagationLotForm, PropagationLotReview } from "../components/PropagationLotForm"
export function PropagationLotPage({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
if (route.id === "novo") return <PropagationLotForm garden={route.garden} saved={record => { saved("Registro salvo. Recarregando dados confirmados."); goToSame(route, record.id, route.search) }} cancel={() => goToSame(route, '', route.search)} />
if (route.id) return <PropagationLotDetail route={route} saved={saved} />
return <PropagationLotList route={route} />
}
function PropagationLotList({ route }: { route: AppRoute }) {
const filters = useListFilters(route, ['isActive'], false)
const options = useLookups(route.garden, [])
const params = { page: filters.page, pageSize: 10, isActive: filters.values.isActive ? filters.values.isActive === "true" : undefined }
const queryKey = JSON.stringify(params)
const load = useCallback((signal: AbortSignal) => propagationLotApi.list(route.garden, { page: filters.page, pageSize: 10, isActive: filters.values.isActive ? filters.values.isActive === "true" : undefined }, signal), [filters.page, filters.values.isActive, route.garden])
const result = useRemote(queryKey + "/" + filters.revision, load)
return <RecordList title="Lotes de origem" hint="Cadastros de apoio com identificação, versão e histórico." create={() => goToSame(route, "novo", route.search)} apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} data={result.data} page={filters.changePage} filters={<>
<SelectField name="isActive" label="Situação cadastral" value={filters.draft.isActive} change={value => filters.change("isActive", value)} options={[{ id: 'true', label: 'Ativo' }, { id: 'false', label: 'Inativo' }]} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}>
{result.data?.items.map(item => <RecordRow key={item.id} href={samePage(route, item.id, route.search)} title={item.origin ?? "Origem não informada"} description={`${enumLabels[item.materialType]} · ${item.receivedOn ? item.receivedOn.split("-").reverse().join("/") : "Data não informada"}`} status={item.isActive ? undefined : "Inativo"} statusTone="invalid" />)}
</RecordList>
}
function PropagationLotDetail({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
const get = useCallback((signal: AbortSignal) => propagationLotApi.get(route.garden, route.id, signal), [route.id, route.garden])
const audit = useCallback((page: number, signal: AbortSignal) => propagationLotApi.history(route.garden, route.id, page, signal), [route.id, route.garden])
const state = useRecord<PropagationLot>(route.id, get, audit, (version, reason) => propagationLotApi.deactivate(route.garden, route.id, version, reason), () => saved("Alteração confirmada."))
const record = state.detail.data
if (route.action === 'editar' && record) return <PropagationLotForm garden={route.garden} original={record} saved={record => { saved("Registro salvo. Dados e indicadores serão recarregados."); const query = new URLSearchParams(route.search); goToSame(route, record.id, query); state.refresh() }} cancel={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query) }} />
return <RecordDetail state={state} title="Lote de origem" version={record?.version ?? 0} active={record?.isActive === true} catalog={true} back={() => { const query = new URLSearchParams(route.search); goToSame(route, '', query) }} edit={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query, 'editar') }} items={record ? [{ label: "Valores do registro", value: <PropagationLotReview record={record} /> }] : []}>{record && <></>}</RecordDetail>
}
