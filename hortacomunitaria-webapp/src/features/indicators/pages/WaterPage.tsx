import { formatDate } from "../../../services/dates"
import { useCallback } from 'react'
import { useListFilters } from '../../../hooks/useListFilters'
import { useLookups } from '../../../hooks/useLookups'
import { useRemote } from '../../../hooks/useRemote'
import { indicatorOriginHref as routeHref } from '../../../services/indicatorOriginHref'
import { irrigationApi } from '../../irrigation/services/irrigationApi'
import { ConsumptionPanel } from '../../irrigation/components/ConsumptionPanel'
import { Field, SelectField } from '../../../components/Fields'
import { Failure } from '../../../components/Feedback'
import { FilterBar } from '../../../components/FilterBar'
import type { RecordRoute } from '../../../types/records'
export function WaterPage({ route }: { route: RecordRoute }) {
  const filters = useListFilters(route, ['from', 'to', 'sectorId'], true), options = useLookups(route.garden, ['irrigationSector'])
  const load = useCallback((signal: AbortSignal) => irrigationApi.consumption(route.garden, { from: filters.values.from, to: filters.values.to, sectorId: filters.values.sectorId, validity: 'Valid' }, filters.page, signal), [route.garden, filters.values.from, filters.values.to, filters.values.sectorId, filters.page])
  const result = useRemote(`${JSON.stringify(filters.values)}/${filters.page}/${filters.revision}`, load)
  const failure = filters.error ?? result.error
  const query = new URLSearchParams({ ...filters.values, validity: 'Valid' })
  return <><div className="page-heading"><div><h1 tabIndex={-1}>Água e irrigação por setor</h1><p>Litros e duração ativa em minutos · competência: início em São Paulo.</p></div></div><FilterBar label="Filtros do indicador" apply={filters.apply} hasError={!!filters.error}><Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change('from', value)} error={filters.error} /><Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change('to', value)} error={filters.error} /><SelectField name="sectorId" label="Setor" value={filters.draft.sectorId} change={value => filters.change('sectorId', value)} options={options.data?.irrigationSector ?? []} />{options.error && <Failure error={options.error} retry={options.retry} />}</FilterBar>{result.loading && <p role="status">Carregando consumo…</p>}{failure && <Failure error={failure} retry={filters.retry} />}{result.data && <><p className="text-[13px] text-horta-muted">{formatDate(result.data.from)} a {formatDate(result.data.to)} · {result.data.definition}</p><ConsumptionPanel data={result.data} changePage={filters.changePage} /><section className="panel flush"><div className="panel-header"><div><h2>Conferir registros de origem</h2><p>Abra as irrigações que compõem este resultado.</p></div><a className="action-link secondary" href={routeHref(route.garden, 'irrigacao', 'registros', '', query)}>Abrir irrigações do período</a></div><ul className="record-results">{result.data.bySector.items.map(item => <li key={item.sectorId}><a href={routeHref(route.garden, 'irrigacao', 'registros', '', new URLSearchParams({ ...filters.values, sectorId: item.sectorId, validity: 'Valid' }))}>Irrigações de {item.name}</a></li>)}</ul></section></>}</>
}
