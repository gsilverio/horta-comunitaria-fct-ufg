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
export function GetBasicCostsPage({ route }: { route: RecordRoute }) {
const filters = useListFilters(route, ['from', 'to', 'category', 'bedId', 'cycleId'], true)
const options = useLookups(route.garden, ['bed', 'cultivationCycle'])
const load = useCallback((signal: AbortSignal) => indicatorApi.getBasicCosts(route.garden, { page: filters.page, pageSize: 5, from: filters.values.from || undefined, to: filters.values.to || undefined, category: filters.values.category || undefined, bedId: filters.values.bedId || undefined, cycleId: filters.values.cycleId || undefined }, signal), [route.garden, filters.page, filters.values.from, filters.values.to, filters.values.category, filters.values.bedId, filters.values.cycleId])
const result = useRemote(JSON.stringify(filters.values) + "/" + filters.page + "/" + filters.revision, load), record = result.data
return <IndicatorPanel title="Custos previstos e realizados" unit="BRL · diferença realizado menos previsto" competence="data de competência" apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} page={filters.changePage} result={record && { from: record.from, to: record.to, definition: record.comparison, availability: record.availability, summary: [{ label: 'Previsto (BRL)', value: record.plannedBrl === null ? "Não informado" : formatNumber(record.plannedBrl) }, { label: 'Realizado (BRL)', value: record.actualBrl === null ? "Não informado" : formatNumber(record.actualBrl) }, { label: 'Diferença (BRL)', value: record.differenceBrl === null ? "Não informado" : formatNumber(record.differenceBrl) }, { label: 'Lançamentos previstos', value: formatNumber(record.plannedCount) }, { label: 'Lançamentos realizados', value: formatNumber(record.actualCount) }, { label: 'Interpretação da comparação', value: record.comparison }], origins: record.origins }} filters={<>
<Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change("from", value)} error={filters.error} />
<Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change("to", value)} error={filters.error} />
<Field name="category" label="Categoria" type="text" value={filters.draft.category} change={value => filters.change("category", value)} />
<SelectField name="bedId" label="Canteiro" value={filters.draft.bedId} change={value => filters.change("bedId", value)} options={options.data?.bed ?? []} />
<SelectField name="cycleId" label="Ciclo" value={filters.draft.cycleId} change={value => filters.change("cycleId", value)} options={options.data?.cultivationCycle ?? []} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}
>{record?.origins.items.map((item, index) => <li key={index} className="rounded-xl border border-horta-line p-4"><Facts items={[{ label: 'Data de competência', value: formatDate(item.entryDate) }, { label: 'Categoria', value: item.category }, { label: 'Natureza', value: enumLabels[item.nature] }, { label: 'Valor (BRL)', value: formatNumber(item.amountBrl) }, { label: 'Canteiro', value: item.bedId === null ? "Não informado" : item.bedId }, { label: 'Ciclo', value: item.cycleId === null ? "Não informado" : item.cycleId }]} /><a className="mt-4 inline-block font-semibold underline" href={routeHref(route.garden, 'custos', 'registros', item.id, new URLSearchParams({ from: filters.values.from, to: filters.values.to, category: filters.values.category, bedId: filters.values.bedId, cycleId: filters.values.cycleId }))}>Abrir origem</a></li>)}</IndicatorPanel>
}
