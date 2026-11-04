import { useCallback } from "react"
import { IndicatorPanel } from "../../../components/IndicatorPanel"
import { Field, SelectField, Facts } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useListFilters } from "../../../hooks/useListFilters"
import { useLookups } from "../../../hooks/useLookups"
import { useRemote } from "../../../hooks/useRemote"
import { indicatorOriginHref as routeHref } from "../../../services/indicatorOriginHref"
import { enumLabels } from "../../../services/labels"
import { formatNumber, formatInstant } from "../../irrigation/formValues"
import type { RecordRoute } from "../../../types/records"
import { indicatorApi } from "../services/indicatorApi"
export function GetOperationalOccurrencesPage({ route }: { route: RecordRoute }) {
const filters = useListFilters(route, ['from', 'to', 'category', 'status'], true)
const options = useLookups(route.garden, [])
const load = useCallback((signal: AbortSignal) => indicatorApi.getOperationalOccurrences(route.garden, { page: filters.page, pageSize: 5, from: filters.values.from || undefined, to: filters.values.to || undefined, category: filters.values.category || undefined, status: filters.values.status || undefined }, signal), [route.garden, filters.page, filters.values.from, filters.values.to, filters.values.category, filters.values.status])
const result = useRemote(JSON.stringify(filters.values) + "/" + filters.page + "/" + filters.revision, load), record = result.data
return <IndicatorPanel title="Ocorrências abertas no período" unit="Ocorrências distintas por situação atual" competence="abertura em São Paulo" apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} page={filters.changePage} result={record && { from: record.from, to: record.to, definition: 'Ocorrências válidas abertas no período, classificadas por sua situação atual. Não representa o estoque de pendências.', availability: record.availability, summary: [{ label: 'Quantidade de origens', value: formatNumber(record.count) }, { label: 'Abertas', value: formatNumber(record.openCount) }, { label: 'Em andamento', value: formatNumber(record.inProgressCount) }, { label: 'Resolvidas', value: formatNumber(record.resolvedCount) }], origins: record.origins }} filters={<>
<Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change("from", value)} error={filters.error} />
<Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change("to", value)} error={filters.error} />
<Field name="category" label="Categoria" type="text" value={filters.draft.category} change={value => filters.change("category", value)} />
<SelectField name="status" label="Situação" value={filters.draft.status} change={value => filters.change("status", value)} options={[{ id: 'Open', label: 'Aberta' }, { id: 'InProgress', label: 'Em andamento' }, { id: 'Resolved', label: 'Resolvida' }]} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}
>{record?.origins.items.map((item, index) => <li key={index} className="rounded-xl border border-horta-line p-4"><Facts items={[{ label: 'Abertura em', value: formatInstant(item.openedAt) }, { label: 'Categoria', value: item.category }, { label: 'Situação', value: enumLabels[item.status] }]} /><a className="mt-4 inline-block font-semibold underline" href={routeHref(route.garden, 'ocorrencias', 'registros', item.id, new URLSearchParams({ from: filters.values.from, to: filters.values.to, category: filters.values.category, status: filters.values.status }))}>Abrir origem</a></li>)}</IndicatorPanel>
}
