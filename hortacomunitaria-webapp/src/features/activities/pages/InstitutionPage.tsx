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

import type { Institution } from "../types/Institution"
import { institutionApi } from "../services/InstitutionApi"
import { InstitutionForm, InstitutionReview } from "../components/InstitutionForm"
export function InstitutionPage({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
if (route.id === "novo") return <InstitutionForm garden={route.garden} saved={record => { saved("Registro salvo. Recarregando dados confirmados."); goToSame(route, record.id, route.search) }} cancel={() => goToSame(route, '', route.search)} />
if (route.id) return <InstitutionDetail route={route} saved={saved} />
return <InstitutionList route={route} />
}
function InstitutionList({ route }: { route: AppRoute }) {
const filters = useListFilters(route, ['isActive'], false)
const options = useLookups(route.garden, [])
const params = { page: filters.page, pageSize: 10, isActive: filters.values.isActive ? filters.values.isActive === "true" : undefined }
const queryKey = JSON.stringify(params)
const load = useCallback((signal: AbortSignal) => institutionApi.list({ page: filters.page, pageSize: 10, isActive: filters.values.isActive ? filters.values.isActive === "true" : undefined }, signal), [filters.page, filters.values.isActive])
const result = useRemote(queryKey + "/" + filters.revision, load)
return <RecordList requireActiveGarden={false} title="Instituições" hint="Cadastros de apoio com identificação, versão e histórico." create={() => goToSame(route, "novo", route.search)} apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} data={result.data} page={filters.changePage} filters={<>
<SelectField name="isActive" label="Situação cadastral" value={filters.draft.isActive} change={value => filters.change("isActive", value)} options={[{ id: 'true', label: 'Ativo' }, { id: 'false', label: 'Inativo' }]} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}>
{result.data?.items.map(item => <RecordRow key={item.id} href={samePage(route, item.id, route.search)} title={item.acronym ? `${item.name} (${item.acronym})` : item.name} description={item.type ?? undefined} status={item.isActive ? undefined : "Inativo"} statusTone="invalid" />)}
</RecordList>
}
function InstitutionDetail({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
const get = useCallback((signal: AbortSignal) => institutionApi.get(route.id, signal), [route.id])
const audit = useCallback((page: number, signal: AbortSignal) => institutionApi.history(route.id, page, signal), [route.id])
const state = useRecord<Institution>(route.id, get, audit, (version, reason) => institutionApi.deactivate(route.id, version, reason), () => saved("Alteração confirmada."))
const record = state.detail.data
if (route.action === 'editar' && record) return <InstitutionForm garden={route.garden} original={record} saved={record => { saved("Registro salvo. Dados e indicadores serão recarregados."); const query = new URLSearchParams(route.search); goToSame(route, record.id, query); state.refresh() }} cancel={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query) }} />
return <RecordDetail requireActiveGarden={false} state={state} title="Instituição" version={record?.version ?? 0} active={record?.isActive === true} catalog={true} back={() => { const query = new URLSearchParams(route.search); goToSame(route, '', query) }} edit={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query, 'editar') }} items={record ? [{ label: "Valores do registro", value: <InstitutionReview record={record} /> }] : []}>{record && <></>}</RecordDetail>
}
