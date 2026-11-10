import { formatDate } from "../../../services/dates"
import { useCallback } from "react"
import { IndicatorPanel } from "../../../components/IndicatorPanel"
import { Field, SelectField, Facts } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useListFilters } from "../../../hooks/useListFilters"
import { useLookups } from "../../../hooks/useLookups"
import { useRemote } from "../../../hooks/useRemote"
import { indicatorOriginHref as routeHref } from "../../../services/indicatorOriginHref"
import { enumLabels } from "../../../services/labels"
import { formatNumber } from "../../irrigation/formValues"
import type { RecordRoute } from "../../../types/records"
import { indicatorApi } from "../services/indicatorApi"
export function GetDryingYieldPage({ route }: { route: RecordRoute }) {
const filters = useListFilters(route, ['from', 'to', 'lotId', 'speciesId'], true)
const options = useLookups(route.garden, ['dryingLot', 'species'])
const load = useCallback((signal: AbortSignal) => indicatorApi.getDryingYield(route.garden, { page: filters.page, pageSize: 5, from: filters.values.from || undefined, to: filters.values.to || undefined, lotId: filters.values.lotId || undefined, speciesId: filters.values.speciesId || undefined }, signal), [route.garden, filters.page, filters.values.from, filters.values.to, filters.values.lotId, filters.values.speciesId])
const result = useRemote(JSON.stringify(filters.values) + "/" + filters.page + "/" + filters.revision, load), record = result.data
return <IndicatorPanel title="Rendimento após secagem" unit="kg e percentual (%)" competence="início do lote" apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} page={filters.changePage} result={record && { from: record.from, to: record.to, definition: record.definition, availability: record.availability, summary: [{ label: 'Massa seca conhecida (kg)', value: record.knownDryMassKg === null ? "Não informado" : formatNumber(record.knownDryMassKg) }, { label: 'Massa destinada elegível (kg)', value: record.eligibleAllocatedMassKg === null ? "Não informado" : formatNumber(record.eligibleAllocatedMassKg) }, { label: 'Percentual (%)', value: record.percentage === null ? "Não informado" : formatNumber(record.percentage) }, { label: 'Lotes elegíveis', value: formatNumber(record.eligibleCount) }, { label: 'Lotes incompletos', value: formatNumber(record.incompleteCount) }, { label: 'Lotes com espécies mistas', value: formatNumber(record.mixedSpeciesCount) }], origins: record.origins }} filters={<>
<Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change("from", value)} error={filters.error} />
<Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change("to", value)} error={filters.error} />
<SelectField name="lotId" label="Lote de secagem" value={filters.draft.lotId} change={value => filters.change("lotId", value)} options={options.data?.dryingLot ?? []} />
<SelectField name="speciesId" label="Espécie" value={filters.draft.speciesId} change={value => filters.change("speciesId", value)} options={options.data?.species ?? []} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}
>{record?.origins.items.map((item, index) => <li key={index} className="border-b border-horta-line py-3"><Facts items={[{ label: 'Data de início', value: formatDate(item.startedOn) }, { label: 'Situação', value: enumLabels[item.status] }, { label: 'Massa destinada (kg)', value: formatNumber(item.allocatedMassKg) }, { label: 'Massa seca total (kg)', value: item.totalDryMassKg === null ? "Não informado" : formatNumber(item.totalDryMassKg) }, { label: 'Espécies mistas', value: item.mixedSpecies ? "Sim" : "Não" }, { label: 'Elegível', value: item.eligible ? "Sim" : "Não" }, { label: 'Percentual (%)', value: item.percentage === null ? "Não informado" : formatNumber(item.percentage) }]} /><a className="mt-4 inline-block font-semibold underline" href={routeHref(route.garden, 'producao', 'secagens', item.id, new URLSearchParams({ from: filters.values.from, to: filters.values.to, lotId: filters.values.lotId, speciesId: filters.values.speciesId }))}>Abrir origem</a></li>)}</IndicatorPanel>
}
