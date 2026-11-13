import type { AppRoute } from '../../../routes/routes'
import { goToSame } from '../../../routes/navigation'
import { samePage } from '../../../routes/routes'
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

import type { EnvironmentalVariable } from "../types/EnvironmentalVariable"
import { environmentalVariableApi } from "../services/EnvironmentalVariableApi"
import { EnvironmentalVariableForm, EnvironmentalVariableReview } from "../components/EnvironmentalVariableForm"
export function EnvironmentalVariablePage({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
if (route.id === "novo") return <EnvironmentalVariableForm garden={route.garden} saved={record => { saved("Registro salvo. Recarregando dados confirmados."); goToSame(route, record.id, route.search) }} cancel={() => goToSame(route, '', route.search)} />
if (route.id) return <EnvironmentalVariableDetail route={route} saved={saved} />
return <EnvironmentalVariableList route={route} />
}
function EnvironmentalVariableList({ route }: { route: AppRoute }) {
const filters = useListFilters(route, ['isActive'], false)
const options = useLookups(route.garden, [])
const params = { page: filters.page, pageSize: 10, isActive: filters.values.isActive ? filters.values.isActive === "true" : undefined }
const queryKey = JSON.stringify(params)
const load = useCallback((signal: AbortSignal) => environmentalVariableApi.list({ page: filters.page, pageSize: 10, isActive: filters.values.isActive ? filters.values.isActive === "true" : undefined }, signal), [filters.page, filters.values.isActive])
const result = useRemote(queryKey + "/" + filters.revision, load)
return <RecordList requireActiveGarden={false} title="Variáveis ambientais" hint="Cadastros de apoio com identificação, versão e histórico." create={() => goToSame(route, "novo", route.search)} apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} data={result.data} page={filters.changePage} filters={<>
<SelectField name="isActive" label="Situação cadastral" value={filters.draft.isActive} change={value => filters.change("isActive", value)} options={[{ id: 'true', label: 'Ativo' }, { id: 'false', label: 'Inativo' }]} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}>
{result.data?.items.map(item => <RecordRow key={item.id} href={samePage(route, item.id, route.search)} title={item.name} description={`${item.quantity} · ${item.referenceUnit}`} status={item.isActive ? undefined : "Inativo"} statusTone="invalid" />)}
</RecordList>
}
function EnvironmentalVariableDetail({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
const get = useCallback((signal: AbortSignal) => environmentalVariableApi.get(route.id, signal), [route.id])
const audit = useCallback((page: number, signal: AbortSignal) => environmentalVariableApi.history(route.id, page, signal), [route.id])
const state = useRecord<EnvironmentalVariable>(route.id, get, audit, (version, reason) => environmentalVariableApi.deactivate(route.id, version, reason), () => saved("Alteração confirmada."))
const record = state.detail.data
if (route.action === 'editar' && record) return <EnvironmentalVariableForm garden={route.garden} original={record} saved={record => { saved("Registro salvo. Dados e indicadores serão recarregados."); const query = new URLSearchParams(route.search); goToSame(route, record.id, query); state.refresh() }} cancel={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query) }} />
return <RecordDetail requireActiveGarden={false} state={state} eyebrow="Variável ambiental" title={(record?.name) || "Variável ambiental"} description={record && `${record.quantity} · ${record.referenceUnit}${record.isSoilMoisture ? ' · umidade do solo' : ''}`} version={record?.version ?? 0} active={record?.isActive === true} catalog={true} back={() => { const query = new URLSearchParams(route.search); goToSame(route, '', query) }} edit={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query, 'editar') }} items={record ? [{ label: "Dados da variável", value: <EnvironmentalVariableReview record={record} /> }] : []}>{record && <></>}</RecordDetail>
}
