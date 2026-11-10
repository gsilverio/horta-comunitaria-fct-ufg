import { useCallback, useMemo, useState } from 'react'
import { dashboardIndicators } from '../services/dashboardApi'
import type { DashboardIndicator } from '../services/dashboardApi'
import { useRemote } from '../../../hooks/useRemote'
import { useGardenState } from '../../../hooks/useGardenState'
import { useListFilters } from '../../../hooks/useListFilters'
import { routeHref } from '../../../hooks/useRoute'
import { Field } from '../../../components/Fields'
import { Failure } from '../../../components/Feedback'
import { FilterBar } from '../../../components/FilterBar'
import { GardenJourney } from '../components/GardenJourney'
import { formatNumber } from '../../irrigation/formValues'
import { formatDate } from '../../../services/dates'
import { today } from '../../irrigation/formValues'
import type { RecordRoute } from '../../../types/records'
import { GardenOverviewScene } from '../../interactive-garden/components/GardenOverviewScene'
import { createGardenProjection } from '../../interactive-garden/projection'
import { interactiveGardenApi } from '../../interactive-garden/services/interactiveGardenApi'
import { gardenCropsApi } from '../../interactive-garden/services/gardenCropsApi'
const availability = { NoData: 'Sem dados', Unavailable: 'Indisponível', Partial: 'Informação parcial', Available: 'Disponível' }
export function OverviewPage({ route }: { route: RecordRoute }) {
  const filters = useListFilters(route, ['from', 'to'], true)
  const workspace = useGardenState()
  const on = today()
  const loadInteractive = useCallback((signal: AbortSignal) => interactiveGardenApi.get(route.garden, on, signal), [on, route.garden])
  const organization = useRemote(`${route.garden}/${on}`, loadInteractive)
  const loadCrops = useCallback((signal: AbortSignal) => gardenCropsApi.list(route.garden, on, signal), [on, route.garden])
  const crops = useRemote(`${route.garden}/${on}`, loadCrops)
  const projection = useMemo(() => createGardenProjection(workspace, organization.data), [organization.data, workspace])
  const draft = projection.garden
  return <><div className="page-heading"><div><span className="eyebrow">{workspace.name}</span><h1 tabIndex={-1}>Dashboard da horta</h1></div></div>
    <GardenJourney garden={route.garden} sectors={organization.data?.sectors.length} beds={organization.data?.beds} crops={crops.data?.length} hasLayout={organization.data ? Boolean(draft.boundary || draft.elements.length > 0) : undefined} />
    <section className="garden-overview" aria-labelledby="garden-overview-title">
      {organization.loading && <p className="hint" role="status">Carregando setores e canteiros da horta…</p>}
      {organization.error && <Failure error={organization.error} />}
      {projection.warnings.map((warning) => <p className="notice" role="status" key={warning}>{warning}</p>)}
      {crops.loading && <p role="status">Carregando plantios da cena…</p>}
      {crops.error && <Failure error={crops.error} />}
      {organization.data && !organization.loading && !organization.error && (draft.boundary || draft.elements.length > 0 ? <GardenOverviewScene key={route.garden} gardenId={route.garden} garden={draft} crops={crops.data} heading={<><h2 id="garden-overview-title">Visão panorâmica da horta</h2><p>{workspace.name} · cena de {formatDate(on)}</p></>} actions={<a className="scene-hud-button" href={routeHref(route.garden, 'horta', 'interativa')}>Editar desenho</a>} legendNote={<p>Baixa, média e alta densidade são ilustrações da quantidade inicial por área ocupada; cada desenho pode representar várias plantas. Reposições não aumentam essa referência. Crescimento e colheita prevista dependem dos prazos configurados, sem confirmar maturidade. Posições e formas mostram a última configuração salva do mapa.</p>} /> : <div className="panel empty"><h2 id="garden-overview-title" className="text-[15px]">Visão panorâmica da horta</h2><p>Nenhum terreno salvo. Desenhe e salve a configuração da horta para vê-la aqui.</p><a className="action-link" href={routeHref(route.garden, 'horta', 'interativa')}>Abrir o mapa da horta</a></div>)}
    </section>
    <h2 className="mb-2">Indicadores do período</h2>
    <FilterBar label="Período do dashboard" apply={filters.apply} hasError={!!filters.error} submitLabel="Atualizar dashboard"><Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change('from', value)} error={filters.error} /><Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change('to', value)} error={filters.error} /></FilterBar>
    <p className="muted text-[13px]">{formatDate(filters.values.from)} a {formatDate(filters.values.to)}. A data considerada está indicada em cada cartão. Sem coleta não significa zero.</p>
    {['Ambiente e água', 'Cultivo e produção', 'Pessoas e operação', 'Economia'].map(group => <section key={group} className="dashboard-group" aria-label={group}><h2>{group}</h2><div className="dashboard-grid">{dashboardIndicators.filter(item => item.group === group).map(item => <OverviewCard key={`${route.garden}/${item.id}/${filters.revision}`} item={item} garden={route.garden} from={filters.values.from} to={filters.values.to} />)}</div></section>)}
  </>
}
function OverviewCard({ item, garden, from, to }: { item: DashboardIndicator; garden: string; from: string; to: string }) {
  const [revision, setRevision] = useState(0)
  const load = useCallback((signal: AbortSignal) => item.load(garden, { from, to }, signal), [item, garden, from, to])
  const result = useRemote(`${garden}/${from}/${to}/${revision}`, load), data = result.data
  const max = Math.max(1, ...(data?.bars?.map(bar => bar.value ?? 0) ?? []))
  return <article className="dashboard-card" aria-label={item.title}><div className="flex flex-wrap items-start justify-between gap-2"><h3>{item.title}</h3>{data && <span className={data.availability === 'Available' ? 'badge' : 'badge warning'}>{availability[data.availability]}</span>}</div><p className="hint">{item.competence}</p>
    {result.loading && <p role="status">Carregando indicador…</p>}{result.error && <Failure error={result.error} retry={() => setRevision(value => value + 1)} />}
    {data && <><p className="dashboard-value">{data.availability === 'NoData' ? 'Sem dados' : data.value}</p><p>{data.note}</p>{data.bars && data.availability !== 'NoData' && <ul className="dashboard-bars">{data.bars.map(bar => <li key={bar.label}><span>{bar.label}</span><strong>{bar.value === null ? 'Não informado' : `${formatNumber(bar.value)} ${data.unit ?? ''}`}</strong>{bar.value !== null && <meter aria-label={`${bar.label} (${data.unit || 'quantidade'})`} value={bar.value} min={0} max={max} />}</li>)}</ul>}{data.chartNote && <p className="hint">{data.chartNote}</p>}</>}
    <a className="dashboard-detail" href={routeHref(garden, 'visao-geral', item.id, '', new URLSearchParams({ from, to }))}>Detalhar {item.title.toLowerCase()} →</a>
  </article>
}
