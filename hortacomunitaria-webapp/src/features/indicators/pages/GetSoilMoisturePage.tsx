import { useCallback } from "react"
import { IndicatorPanel } from "../../../components/IndicatorPanel"
import { Field, SelectField, Facts } from "../../../components/Fields"
import { Failure, Pagination } from "../../../components/Feedback"
import { useListFilters } from "../../../hooks/useListFilters"
import { useLookups } from "../../../hooks/useLookups"
import { useRemote } from "../../../hooks/useRemote"
import { indicatorOriginHref as routeHref } from "../../../services/indicatorOriginHref"
import { enumLabels } from "../../../services/labels"
import { formatNumber, formatInstant } from "../../irrigation/formValues"
import type { RecordRoute } from "../../../types/records"
import { indicatorApi } from "../services/indicatorApi"
export function GetSoilMoisturePage({ route }: { route: RecordRoute }) {
const filters = useListFilters(route, ['from', 'to', 'pointId', 'unit', 'method'], true)
const options = useLookups(route.garden, ['samplingPoint'])
const load = useCallback((signal: AbortSignal) => indicatorApi.getSoilMoisture(route.garden, { page: filters.page, pageSize: 5, from: filters.values.from || undefined, to: filters.values.to || undefined, pointId: filters.values.pointId || undefined, unit: filters.values.unit || undefined, method: filters.values.method || undefined }, signal), [route.garden, filters.page, filters.values.from, filters.values.to, filters.values.pointId, filters.values.unit, filters.values.method])
const result = useRemote(JSON.stringify(filters.values) + "/" + filters.page + "/" + filters.revision, load), record = result.data
return <IndicatorPanel title="Umidade do solo" unit="Valor e unidade por método" competence="coleta em São Paulo" apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} page={filters.changePage} result={record && { from: record.from, to: record.to, definition: record.definition, availability: record.availability, summary: [{ label: 'Coletas normais', value: formatNumber(record.normalCount) }, { label: 'Coletas questionáveis', value: formatNumber(record.questionableCount) }], origins: record.origins }} filters={<>
<Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change("from", value)} error={filters.error} />
<Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change("to", value)} error={filters.error} />
<SelectField name="pointId" label="Ponto de coleta" value={filters.draft.pointId} change={value => filters.change("pointId", value)} options={options.data?.samplingPoint ?? []} />
<Field name="unit" label="Unidade" type="text" value={filters.draft.unit} change={value => filters.change("unit", value)} />
<Field name="method" label="Método" type="text" value={filters.draft.method} change={value => filters.change("method", value)} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}
groups={record && <section className="panel"><h2>Grupos compatíveis do período</h2>{record.groups.items.map((item, index) => <div className="border-b border-horta-line py-4" key={index}><Facts items={[{ label: 'Ponto de coleta', value: item.pointId }, { label: 'Unidade', value: item.unit }, { label: 'Método', value: item.method }, { label: 'Quantidade de origens', value: formatNumber(item.count) }, { label: 'Coletas normais', value: formatNumber(item.normalCount) }, { label: 'Coletas questionáveis', value: formatNumber(item.questionableCount) }, { label: 'Última observação do período', value: item.lastObservation ? <Facts items={[{ label: "Coleta", value: formatInstant(item.lastObservation.collectedAt) }, { label: "Valor", value: formatNumber(item.lastObservation.value) + " " + item.lastObservation.unit }, { label: "Qualidade", value: enumLabels[item.lastObservation.quality] }]} /> : "Não informado" }]} /></div>)}<Pagination data={record.groups} change={filters.changePage} label="Páginas de grupos" /></section>}
>{record?.origins.items.map((item, index) => <li key={index} className="rounded-xl border border-horta-line p-4"><Facts items={[{ label: 'Ponto de coleta', value: item.pointId }, { label: 'Coletado em', value: formatInstant(item.collectedAt) }, { label: 'Valor medido', value: formatNumber(item.value) }, { label: 'Unidade', value: item.unit }, { label: 'Método', value: item.method }, { label: 'Qualidade', value: enumLabels[item.quality] }]} /><a className="mt-4 inline-block font-semibold underline" href={routeHref(route.garden, 'ambiente', 'medicoes', item.id, new URLSearchParams({ from: filters.values.from, to: filters.values.to, pointId: filters.values.pointId, unit: filters.values.unit, method: filters.values.method }))}>Abrir origem</a></li>)}</IndicatorPanel>
}
