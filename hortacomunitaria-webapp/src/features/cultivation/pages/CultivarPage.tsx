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
import type { Cultivar } from "../types/Cultivar"
import { cultivarApi } from "../services/CultivarApi"
import { CultivarForm, CultivarReview } from "../components/CultivarForm"
export function CultivarPage({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
const parent = route.search.get("speciesId") || ""
if (route.id === "novo" && !parent) return <section className="panel"><h2>Selecione a espécie primeiro</h2><p>O cultivar pertence a uma espécie. Selecione-a na consulta antes de criar o cadastro.</p><button onClick={() => navigate(route.garden, route.module, route.resource)}>Voltar à seleção</button></section>
if (route.id === "novo") return <CultivarForm garden={route.garden} parent={parent} saved={record => { saved("Registro salvo. Recarregando dados confirmados."); navigate(route.garden, route.module, route.resource, record.id, route.search) }} cancel={() => navigate(route.garden, route.module, route.resource, "", route.search)} />
if (route.id) return <CultivarDetail route={route} saved={saved} />
return <CultivarList route={route} />
}
function CultivarList({ route }: { route: RecordRoute }) {
const parent = route.search.get("speciesId") || ""
const filters = useListFilters(route, ['isActive'], false)
const options = useLookups(route.garden, ['species'])
const params = { page: filters.page, pageSize: 10, isActive: filters.values.isActive ? filters.values.isActive === "true" : undefined }
const queryKey = JSON.stringify(params)
const load = useCallback((signal: AbortSignal) => parent ? cultivarApi.list(parent, { page: filters.page, pageSize: 10, isActive: filters.values.isActive ? filters.values.isActive === "true" : undefined }, signal) : Promise.resolve({ items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0, hasPreviousPage: false, hasNextPage: false }), [filters.page, filters.values.isActive, parent])
const result = useRemote(queryKey + "/" + filters.revision + "/" + parent, load)
return <RecordList createDisabled={!parent || options.loading || !!options.error} requireActiveGarden={false} title="Cultivares" hint="Cadastros de apoio com identificação, versão e histórico." create={() => navigate(route.garden, route.module, route.resource, "novo", route.search)} apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} data={result.data} page={filters.changePage} filters={<>
<SelectField name="speciesId" label="Espécie dos cultivares" required value={parent} change={value => { const query = new URLSearchParams(route.search); query.set("speciesId", value); navigate(route.garden, route.module, route.resource, "", query) }} options={options.data?.species ?? []} />
<SelectField name="isActive" label="Situação cadastral" value={filters.draft.isActive} change={value => filters.change("isActive", value)} options={[{ id: 'true', label: 'Ativo' }, { id: 'false', label: 'Inativo' }]} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}>
{result.data?.items.map(item => <li className="py-4" key={item.id}><a className="font-semibold underline decoration-horta-line underline-offset-4" href={routeHref(route.garden, route.module, route.resource, item.id, route.search)}>{item.name}</a><span className="ml-3 text-sm text-horta-muted">{item.isActive ? "Ativo" : "Inativo"} · v{item.version}</span></li>)}
</RecordList>
}
function CultivarDetail({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
const parent = route.search.get("speciesId") || ""
const get = useCallback((signal: AbortSignal) => cultivarApi.get(parent, route.id, signal), [route.id, parent])
const audit = useCallback((page: number, signal: AbortSignal) => cultivarApi.history(parent, route.id, page, signal), [route.id, parent])
const state = useRecord<Cultivar>(route.id, get, audit, (version, reason) => cultivarApi.deactivate(parent, route.id, version, reason), () => saved("Alteração confirmada."))
const record = state.detail.data
const leaveVisual = () => { const query = new URLSearchParams(route.search); query.delete("visual"); navigate(route.garden, route.module, route.resource, route.id, query) }
if (route.search.get("visual") === "1" && record?.isActive) return <VisualGrowthForm original={record} correct={(values, version, reason) => visualGrowthApi.cultivar(parent, record.id, values, version, reason)} reload={() => cultivarApi.get(parent, record.id)} saved={() => { saved("Prazos visuais salvos."); leaveVisual(); state.refresh() }} cancel={leaveVisual} />
if (route.search.get("edit") === "1" && record) return <CultivarForm garden={route.garden} parent={parent} original={record} saved={record => { saved("Registro salvo. Dados e indicadores serão recarregados."); const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, record.id, query); state.refresh() }} cancel={() => { const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, route.id, query) }} />
return <RecordDetail requireActiveGarden={false} state={state} title="Cultivar" version={record?.version ?? 0} active={record?.isActive === true} catalog={true} back={() => { const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, "", query) }} edit={() => { const query = new URLSearchParams(route.search); query.set("edit", "1"); navigate(route.garden, route.module, route.resource, route.id, query) }} items={record ? [{ label: "Valores do registro", value: <CultivarReview record={record} /> }] : []}>{record && <section className="mt-5 border-t border-horta-line pt-5"><h2>Fases no panorama</h2><p>{record.visualGrowthDays === null ? "Prazos próprios não configurados. Cultivares podem herdar a configuração da espécie." : `Crescimento após ${record.visualGrowthDays} dias; colheita prevista após ${record.visualHarvestDays} dias do plantio.`}</p>{record.isActive && <a className="action-link secondary" href={routeHref(route.garden, route.module, route.resource, record.id, new URLSearchParams({ ...Object.fromEntries(route.search), visual: "1" }))}>Configurar fases visuais</a>}</section>}</RecordDetail>
}
