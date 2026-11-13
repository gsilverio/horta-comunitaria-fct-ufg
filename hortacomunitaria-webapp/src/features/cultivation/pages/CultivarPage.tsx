import type { AppRoute } from '../../../routes/routes'
import { goToSame } from '../../../routes/navigation'
import { samePage } from '../../../routes/routes'
import { CropCatalogEntry } from '../components/CropCatalogEntry'
import { speciesApi } from '../services/SpeciesApi'
import { useSpeciesImage } from '../../interactive-garden/hooks/useSpeciesCatalog'
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

import type { Cultivar } from "../types/Cultivar"
import { cultivarApi } from "../services/CultivarApi"
import { CultivarForm, CultivarReview } from "../components/CultivarForm"
export function CultivarPage({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
const parent = route.search.get("speciesId") || ""
if (route.id === "novo" && !parent) return <section className="panel"><h2>Selecione a espécie primeiro</h2><p>O cultivar pertence a uma espécie. Selecione-a na consulta antes de criar o cadastro.</p><button onClick={() => goToSame(route)}>Voltar à seleção</button></section>
if (route.id === "novo") return <CultivarForm garden={route.garden} parent={parent} saved={record => { saved("Registro salvo. Recarregando dados confirmados."); goToSame(route, record.id, route.search) }} cancel={() => goToSame(route, '', route.search)} />
if (route.id) return <CultivarDetail route={route} saved={saved} />
return <CultivarList route={route} />
}
function CultivarList({ route }: { route: AppRoute }) {
const parent = route.search.get("speciesId") || ""
const filters = useListFilters(route, ['isActive'], false)
const options = useLookups(route.garden, ['species'])
const params = { page: filters.page, pageSize: 10, isActive: filters.values.isActive ? filters.values.isActive === "true" : undefined }
const queryKey = JSON.stringify(params)
const load = useCallback((signal: AbortSignal) => parent ? cultivarApi.list(parent, { page: filters.page, pageSize: 10, isActive: filters.values.isActive ? filters.values.isActive === "true" : undefined }, signal) : Promise.resolve({ items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0, hasPreviousPage: false, hasNextPage: false }), [filters.page, filters.values.isActive, parent])
const result = useRemote(queryKey + "/" + filters.revision + "/" + parent, load)
const loadSpecies = useCallback((signal: AbortSignal) => parent ? speciesApi.get(parent, signal) : Promise.resolve(undefined), [parent])
const species = useRemote(`cultivar-species/${parent}`, loadSpecies)
const image = useSpeciesImage(parent, !!species.data?.imageUrl)
return <RecordList createDisabled={!parent || options.loading || !!options.error} requireActiveGarden={false} title="Cultivares" hint="Escolha uma espécie para consultar suas cultivares. As imagens são ilustrativas da espécie." create={() => goToSame(route, "novo", route.search)} apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} data={result.data} page={filters.changePage} filters={<>
<SelectField name="speciesId" label="Espécie dos cultivares" required value={parent} change={value => { const query = new URLSearchParams(route.search); query.set("speciesId", value); query.delete("page"); goToSame(route, '', query) }} options={options.data?.species ?? []} />
<SelectField name="isActive" label="Situação cadastral" value={filters.draft.isActive} change={value => filters.change("isActive", value)} options={[{ id: 'true', label: 'Ativo' }, { id: 'false', label: 'Inativo' }]} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}>
{result.data?.items.map(item => <CropCatalogEntry key={item.id} name={item.name} scientificName={species.data?.scientificName} active={item.isActive} speciesPhoto image={image} href={samePage(route, item.id, route.search)} />)}
</RecordList>
}
function CultivarDetail({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
const parent = route.search.get("speciesId") || ""
const get = useCallback((signal: AbortSignal) => cultivarApi.get(parent, route.id, signal), [route.id, parent])
const audit = useCallback((page: number, signal: AbortSignal) => cultivarApi.history(parent, route.id, page, signal), [route.id, parent])
const state = useRecord<Cultivar>(route.id, get, audit, (version, reason) => cultivarApi.deactivate(parent, route.id, version, reason), () => saved("Alteração confirmada."))
const record = state.detail.data
const leaveVisual = () => goToSame(route, route.id, route.search)
if (route.action === 'fases' && record?.isActive) return <VisualGrowthForm original={record} correct={(values, version, reason) => visualGrowthApi.cultivar(parent, record.id, values, version, reason)} reload={() => cultivarApi.get(parent, record.id)} saved={() => { saved("Prazos visuais salvos."); leaveVisual(); state.refresh() }} cancel={leaveVisual} />
if (route.action === 'editar' && record) return <CultivarForm garden={route.garden} parent={parent} original={record} saved={record => { saved("Registro salvo. Dados e indicadores serão recarregados."); const query = new URLSearchParams(route.search); goToSame(route, record.id, query); state.refresh() }} cancel={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query) }} />
return <RecordDetail requireActiveGarden={false} state={state} eyebrow="Cultivar" title={(record?.name) || "Cultivar"} description={undefined} version={record?.version ?? 0} active={record?.isActive === true} catalog={true} back={() => goToSame(route, '', route.search)} edit={() => goToSame(route, route.id, route.search, 'editar')} items={record ? [{ label: "Dados da cultivar", value: <CultivarReview record={record} /> }] : []}>{record && <section className="mt-5 border-t border-horta-line pt-5"><h2>Fases no panorama</h2><p>{record.visualGrowthDays === null ? "Prazos próprios não configurados. Cultivares podem herdar a configuração da espécie." : `Crescimento após ${record.visualGrowthDays} dias; colheita prevista após ${record.visualHarvestDays} dias do plantio.`}</p>{record.isActive && <a className="action-link secondary" href={samePage(route, record.id, route.search, 'fases')}>Configurar fases visuais</a>}</section>}</RecordDetail>
}
