import { SpeciesProfile, SpeciesVisualStages } from '../components/SpeciesProfile'
import { SpeciesCatalogEntry } from '../components/CropCatalogEntry'
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
import { SpeciesForm } from "../components/SpeciesForm"
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
return <RecordList requireActiveGarden={false} title="Espécies" hint="Consulte as espécies do catálogo, suas imagens e os dados de cultivo." create={() => navigate(route.garden, route.module, route.resource, "novo", route.search)} apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} data={result.data} page={filters.changePage} filters={<>
<SelectField name="isActive" label="Situação cadastral" value={filters.draft.isActive} change={value => filters.change("isActive", value)} options={[{ id: 'true', label: 'Ativo' }, { id: 'false', label: 'Inativo' }]} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}>
{result.data?.items.map(item => <SpeciesCatalogEntry key={item.id} species={item} href={routeHref(route.garden, route.module, route.resource, item.id, route.search)} />)}
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
return <RecordDetail compact requireActiveGarden={false} state={state} title={record?.commonName ?? 'Espécie'} heading={record ? <SpeciesProfile species={record} route={route} /> : undefined} version={record?.version ?? 0} active={record?.isActive === true} catalog back={() => { const query = new URLSearchParams(route.search); query.delete('edit'); navigate(route.garden, route.module, route.resource, '', query) }} edit={() => { const query = new URLSearchParams(route.search); query.set('edit', '1'); navigate(route.garden, route.module, route.resource, route.id, query) }} items={[]}>
  {record && <SpeciesVisualStages species={record} route={route} />}
</RecordDetail>
}
