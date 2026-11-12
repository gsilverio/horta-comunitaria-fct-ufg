import type { AppRoute } from '../../../routes/routes'
import { goToSame } from '../../../routes/navigation'
import { samePage } from '../../../routes/routes'
import { formatDate } from "../../../services/dates"
import { RecordRow } from '../../../components/RecordRow'
import { formatNumber } from "../../irrigation/formValues"
import { formatInstant } from "../../irrigation/formValues"
import { useCallback } from "react"
import { RecordList } from "../../../components/RecordList"
import { RecordDetail } from "../../../components/RecordDetail"
import { Field, SelectField } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useRecord } from "../../../hooks/useRecord"
import { useRemote } from "../../../hooks/useRemote"
import { useLookups } from "../../../hooks/useLookups"
import { useListFilters } from "../../../hooks/useListFilters"

import type { FinancialEntry } from "../types/FinancialEntry"
import { financialEntryApi } from "../services/FinancialEntryApi"
import { FinancialEntryForm, FinancialEntryReview } from "../components/FinancialEntryForm"
import { enumLabels } from "../../../services/labels"
export function FinancialEntryPage({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
if (route.id === "novo") return <FinancialEntryForm garden={route.garden} saved={record => { saved("Registro salvo. Recarregando dados confirmados."); goToSame(route, record.id, route.search) }} cancel={() => goToSame(route, '', route.search)} />
if (route.id) return <FinancialEntryDetail route={route} saved={saved} />
return <FinancialEntryList route={route} />
}
function FinancialEntryList({ route }: { route: AppRoute }) {
const filters = useListFilters(route, ['from', 'to', 'validity', 'category', 'nature', 'bedId', 'cycleId'], true)
const options = useLookups(route.garden, ['bed', 'cultivationCycle'])
const params = { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, category: filters.values.category || undefined, nature: filters.values.nature || undefined, bedId: filters.values.bedId || undefined, cycleId: filters.values.cycleId || undefined }
const queryKey = JSON.stringify(params)
const load = useCallback((signal: AbortSignal) => financialEntryApi.list(route.garden, { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, category: filters.values.category || undefined, nature: filters.values.nature || undefined, bedId: filters.values.bedId || undefined, cycleId: filters.values.cycleId || undefined }, signal), [filters.page, filters.values.from, filters.values.to, filters.values.validity, filters.values.category, filters.values.nature, filters.values.bedId, filters.values.cycleId, route.garden])
const result = useRemote(queryKey + "/" + filters.revision, load)
return <RecordList title="Lançamentos financeiros" hint="Período da data do registro. Consulte a origem, corrija com motivo e preserve o histórico." create={() => goToSame(route, "novo", route.search)} apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} data={result.data} page={filters.changePage} filters={<>
<Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change("from", value)} error={filters.error} />
<Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change("to", value)} error={filters.error} />
<SelectField name="validity" label="Validade" value={filters.draft.validity} change={value => filters.change("validity", value)} options={[{ id: 'Valid', label: enumLabels['Valid'] }, { id: 'Invalid', label: enumLabels['Invalid'] }]} />
<Field name="category" label="Categoria" type="text" value={filters.draft.category} change={value => filters.change("category", value)} />
<SelectField name="nature" label="Natureza" value={filters.draft.nature} change={value => filters.change("nature", value)} options={[{ id: 'Planned', label: enumLabels['Planned'] }, { id: 'Actual', label: enumLabels['Actual'] }]} />
<SelectField name="bedId" label="Canteiro" value={filters.draft.bedId} change={value => filters.change("bedId", value)} options={options.data?.bed ?? []} />
<SelectField name="cycleId" label="Ciclo" value={filters.draft.cycleId} change={value => filters.change("cycleId", value)} options={options.data?.cultivationCycle ?? []} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}>
{result.data?.items.map(item => <RecordRow key={item.id} href={samePage(route, item.id, route.search)} title={item.description} description={`${enumLabels[item.nature]} · ${item.category} · ${formatDate(item.entryDate)}`} meta={<strong className="text-sm">R$ {formatNumber(item.amountBrl)}</strong>} status={item.validity === "Invalid" ? enumLabels[item.validity] : undefined} statusTone="invalid" />)}
</RecordList>
}
function FinancialEntryDetail({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
const get = useCallback((signal: AbortSignal) => financialEntryApi.get(route.garden, route.id, signal), [route.id, route.garden])
const audit = useCallback((page: number, signal: AbortSignal) => financialEntryApi.history(route.garden, route.id, page, signal), [route.id, route.garden])
const state = useRecord<FinancialEntry>(route.id, get, audit, (version, reason) => financialEntryApi.deactivate(route.garden, route.id, version, reason), () => saved("Alteração confirmada."))
const record = state.detail.data
if (route.action === 'editar' && record) return <FinancialEntryForm garden={route.garden} original={record} saved={record => { saved("Registro salvo. Dados e indicadores serão recarregados."); const query = new URLSearchParams(route.search); goToSame(route, record.id, query); state.refresh() }} cancel={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query) }} />
return <RecordDetail state={state} title="Lançamento financeiro" version={record?.version ?? 0} active={record?.validity === "Valid"} catalog={false} back={() => { const query = new URLSearchParams(route.search); goToSame(route, '', query) }} edit={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query, 'editar') }} items={record ? [{ label: "Valores do registro", value: <FinancialEntryReview record={record} /> }, { label: "Incluído em", value: formatInstant(record.recordedAt) }, { label: "Autor do registro (identificador)", value: record.recordedByUserId }] : []}>{record && <></>}</RecordDetail>
}
