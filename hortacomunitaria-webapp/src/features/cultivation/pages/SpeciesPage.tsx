import { VisualGrowthForm } from '../components/VisualGrowthForm'
import { visualGrowthApi } from '../services/visualGrowthApi'
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
import type { Species } from "../types/Species"
import { speciesApi } from "../services/SpeciesApi"
import { SpeciesForm, SpeciesReview } from "../components/SpeciesForm"
export function SpeciesPage({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
if (route.id === "novo") return <SpeciesForm garden={route.garden} saved={record => { saved("Registro salvo. Recarregando dados confirmados."); navigate(route.garden, route.module, route.resource, record.id, route.search) }} cancel={() => navigate(route.garden, route.module, route.resource, "", route.search)} />
if (route.id) return <SpeciesDetail route={route} saved={saved} />
return <SpeciesList route={route} />
}
function SpeciesList({ route }: { route: RecordRoute }) {
const filters = useListFilters(route, ['isActive'], false)
const options = useLookups(route.garden, [])
const params = { page: filters.page, pageSize: 10, isActive: filters.values.isActive ? filters.values.isActive === "true" : undefined }
const queryKey = JSON.stringify(params)
const load = useCallback((signal: AbortSignal) => speciesApi.list({ page: filters.page, pageSize: 10, isActive: filters.values.isActive ? filters.values.isActive === "true" : undefined }, signal), [filters.page, filters.values.isActive])
const result = useRemote(queryKey + "/" + filters.revision, load)
return <RecordList requireActiveGarden={false} title="Espécies" hint="Cadastros de apoio com identificação, versão e histórico." create={() => navigate(route.garden, route.module, route.resource, "novo", route.search)} apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} data={result.data} page={filters.changePage} filters={<>
<SelectField name="isActive" label="Situação cadastral" value={filters.draft.isActive} change={value => filters.change("isActive", value)} options={[{ id: 'true', label: 'Ativo' }, { id: 'false', label: 'Inativo' }]} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}>
{result.data?.items.map(item => <li className="py-4" key={item.id}><a className="font-semibold underline decoration-horta-line underline-offset-4" href={routeHref(route.garden, route.module, route.resource, item.id, route.search)}>{item.commonName}</a><span className="ml-3 text-sm text-horta-muted">{item.isActive ? "Ativo" : "Inativo"} · v{item.version}</span></li>)}
</RecordList>
}
function SpeciesDetail({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
const get = useCallback((signal: AbortSignal) => speciesApi.get(route.id, signal), [route.id])
const audit = useCallback((page: number, signal: AbortSignal) => speciesApi.history(route.id, page, signal), [route.id])
const state = useRecord<Species>(route.id, get, audit, (version, reason) => speciesApi.deactivate(route.id, version, reason), () => saved("Alteração confirmada."))
const record = state.detail.data
const leaveVisual = () => { const query = new URLSearchParams(route.search); query.delete("visual"); navigate(route.garden, route.module, route.resource, route.id, query) }
if (route.search.get("visual") === "1" && record?.isActive) return <VisualGrowthForm original={record} correct={(values, version, reason) => visualGrowthApi.species(record.id, values, version, reason)} reload={() => speciesApi.get(record.id)} saved={() => { saved("Prazos visuais salvos."); leaveVisual(); state.refresh() }} cancel={leaveVisual} />
if (route.search.get("edit") === "1" && record) return <SpeciesForm garden={route.garden} original={record} saved={record => { saved("Registro salvo. Dados e indicadores serão recarregados."); const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, record.id, query); state.refresh() }} cancel={() => { const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, route.id, query) }} />
return <RecordDetail requireActiveGarden={false} state={state} title="Espécie" version={record?.version ?? 0} active={record?.isActive === true} catalog={true} back={() => { const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, "", query) }} edit={() => { const query = new URLSearchParams(route.search); query.set("edit", "1"); navigate(route.garden, route.module, route.resource, route.id, query) }} items={record ? [{ label: "Valores do registro", value: <SpeciesReview record={record} /> }] : []}>{record && <section className="mt-5 border-t border-horta-line pt-5"><h2>Fases no panorama</h2><p>{record.visualGrowthDays === null ? "Prazos próprios não configurados. Cultivares podem herdar a configuração da espécie." : `Crescimento após ${record.visualGrowthDays} dias; colheita prevista após ${record.visualHarvestDays} dias do plantio.`}</p>{record.isActive && <a className="action-link secondary" href={routeHref(route.garden, route.module, route.resource, record.id, new URLSearchParams({ ...Object.fromEntries(route.search), visual: "1" }))}>Configurar fases visuais</a>}</section>}</RecordDetail>
}
