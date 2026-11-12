import type { AppRoute } from '../../../routes/routes'
import { linkTo, withReturn } from '../../../routes/routes'
import { useCallback } from "react"
import { IndicatorPanel } from "../../../components/IndicatorPanel"
import { Field, SelectField, Facts } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useListFilters } from "../../../hooks/useListFilters"
import { useLookups } from "../../../hooks/useLookups"
import { useRemote } from "../../../hooks/useRemote"
import { formatNumber } from "../../irrigation/formValues"

import { indicatorApi } from "../services/indicatorApi"
export function GetProductivityPage({ route }: { route: AppRoute }) {
const filters = useListFilters(route, ['from', 'to', 'cycleId', 'speciesId'], true)
const options = useLookups(route.garden, ['cultivationCycle', 'species'])
const load = useCallback((signal: AbortSignal) => indicatorApi.getProductivity(route.garden, { page: filters.page, pageSize: 5, from: filters.values.from || undefined, to: filters.values.to || undefined, cycleId: filters.values.cycleId || undefined, speciesId: filters.values.speciesId || undefined }, signal), [route.garden, filters.page, filters.values.from, filters.values.to, filters.values.cycleId, filters.values.speciesId])
const result = useRemote(JSON.stringify(filters.values) + "/" + filters.page + "/" + filters.revision, load), record = result.data
return <IndicatorPanel title="Produtividade" unit="kg e kg/m² por ciclo" competence="data da colheita" apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} page={filters.changePage} result={record && { from: record.from, to: record.to, definition: record.definition, availability: record.availability, summary: [{ label: 'Massa fresca (kg)', value: record.freshMassKg === null ? "Não informado" : formatNumber(record.freshMassKg) }, { label: 'Colheitas', value: formatNumber(record.harvestCount) }], origins: record.origins }} filters={<>
<Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change("from", value)} error={filters.error} />
<Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change("to", value)} error={filters.error} />
<SelectField name="cycleId" label="Ciclo" value={filters.draft.cycleId} change={value => filters.change("cycleId", value)} options={options.data?.cultivationCycle ?? []} />
<SelectField name="speciesId" label="Espécie" value={filters.draft.speciesId} change={value => filters.change("speciesId", value)} options={options.data?.species ?? []} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}
>{record?.origins.items.map((item, index) => <li key={index} className="border-b border-horta-line py-3"><Facts items={[{ label: 'Espécie', value: item.speciesId }, { label: 'Área ocupada (m²)', value: formatNumber(item.occupiedAreaM2) }, { label: 'Massa fresca (kg)', value: formatNumber(item.freshMassKg) }, { label: 'Colheitas', value: formatNumber(item.harvestCount) }, { label: 'Produtividade (kg/m²)', value: formatNumber(item.productivityKgPerM2) }]} /><a className="mt-4 inline-block font-semibold underline" href={linkTo(route.garden, 'cultivo/ciclos', item.cycleId, withReturn(new URLSearchParams({ from: filters.values.from, to: filters.values.to, cycleId: filters.values.cycleId, speciesId: filters.values.speciesId })))}>Abrir origem</a></li>)}</IndicatorPanel>
}
