import { EvidenceSection } from "../../evidence/pages/EvidenceSection"
import { RecordRow } from '../../../components/RecordRow'
import { choiceLabel } from '../../../services/lookups'
import { formatDate } from "../../../services/dates"
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
import { navigate, routeHref } from "../../../hooks/useRoute"
import type { RecordRoute } from "../../../types/records"
import type { AgronomicObservation } from "../types/AgronomicObservation"
import { agronomicObservationApi } from "../services/AgronomicObservationApi"
import { AgronomicObservationForm, AgronomicObservationReview } from "../components/AgronomicObservationForm"
import { enumLabels } from "../../../services/labels"
export function AgronomicObservationPage({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
if (route.id === "novo") return <AgronomicObservationForm garden={route.garden} saved={record => { saved("Registro salvo. Recarregando dados confirmados."); navigate(route.garden, route.module, route.resource, record.id, route.search) }} cancel={() => navigate(route.garden, route.module, route.resource, "", route.search)} />
if (route.id) return <AgronomicObservationDetail route={route} saved={saved} />
return <AgronomicObservationList route={route} />
}
function AgronomicObservationList({ route }: { route: RecordRoute }) {
const filters = useListFilters(route, ['from', 'to', 'validity', 'cycleId', 'category', 'bedId', 'speciesId'], true)
const options = useLookups(route.garden, ['bed', 'cultivationCycle', 'species'])
const params = { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, cycleId: filters.values.cycleId || undefined, category: filters.values.category || undefined, bedId: filters.values.bedId || undefined, speciesId: filters.values.speciesId || undefined }
const queryKey = JSON.stringify(params)
const load = useCallback((signal: AbortSignal) => agronomicObservationApi.list(route.garden, { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, cycleId: filters.values.cycleId || undefined, category: filters.values.category || undefined, bedId: filters.values.bedId || undefined, speciesId: filters.values.speciesId || undefined }, signal), [filters.page, filters.values.from, filters.values.to, filters.values.validity, filters.values.cycleId, filters.values.category, filters.values.bedId, filters.values.speciesId, route.garden])
const result = useRemote(queryKey + "/" + filters.revision, load)
return <RecordList title="Observações agronômicas" hint="Período da data do registro. Consulte a origem, corrija com motivo e preserve o histórico." create={() => navigate(route.garden, route.module, route.resource, "novo", route.search)} apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} data={result.data} page={filters.changePage} filters={<>
<Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change("from", value)} error={filters.error} />
<Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change("to", value)} error={filters.error} />
<SelectField name="validity" label="Validade" value={filters.draft.validity} change={value => filters.change("validity", value)} options={[{ id: 'Valid', label: enumLabels['Valid'] }, { id: 'Invalid', label: enumLabels['Invalid'] }]} />
<SelectField name="cycleId" label="Ciclo" value={filters.draft.cycleId} change={value => filters.change("cycleId", value)} options={options.data?.cultivationCycle ?? []} />
<Field name="category" label="Categoria" type="text" value={filters.draft.category} change={value => filters.change("category", value)} />
<SelectField name="bedId" label="Canteiro" value={filters.draft.bedId} change={value => filters.change("bedId", value)} options={options.data?.bed ?? []} />
<SelectField name="speciesId" label="Espécie" value={filters.draft.speciesId} change={value => filters.change("speciesId", value)} options={options.data?.species ?? []} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}>
{result.data?.items.map(item => <RecordRow key={item.id} href={routeHref(route.garden, route.module, route.resource, item.id, route.search)} title={item.category} description={`${formatDate(item.observedOn)} · ${choiceLabel(options.data?.cultivationCycle, item.cycleId) ?? "Ciclo"} · ${item.description}`} status={item.validity === "Invalid" ? enumLabels[item.validity] : undefined} statusTone="invalid" />)}
</RecordList>
}
function AgronomicObservationDetail({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
const get = useCallback((signal: AbortSignal) => agronomicObservationApi.get(route.garden, route.id, signal), [route.id, route.garden])
const audit = useCallback((page: number, signal: AbortSignal) => agronomicObservationApi.history(route.garden, route.id, page, signal), [route.id, route.garden])
const state = useRecord<AgronomicObservation>(route.id, get, audit, (version, reason) => agronomicObservationApi.deactivate(route.garden, route.id, version, reason), () => saved("Alteração confirmada."))
const record = state.detail.data
if (route.search.get("edit") === "1" && record) return <AgronomicObservationForm garden={route.garden} original={record} saved={record => { saved("Registro salvo. Dados e indicadores serão recarregados."); const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, record.id, query); state.refresh() }} cancel={() => { const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, route.id, query) }} />
return <RecordDetail attachments={record && <EvidenceSection garden={route.garden} targetType="AgronomicObservation" targetId={record.id} active={record.validity === "Valid"} />} state={state} title="Observação agronômica" version={record?.version ?? 0} active={record?.validity === "Valid"} catalog={false} back={() => { const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, "", query) }} edit={() => { const query = new URLSearchParams(route.search); query.set("edit", "1"); navigate(route.garden, route.module, route.resource, route.id, query) }} items={record ? [{ label: "Valores do registro", value: <AgronomicObservationReview record={record} /> }, { label: "Incluído em", value: formatInstant(record.recordedAt) }, { label: "Autor técnico (demonstração)", value: record.recordedByUserId }] : []}>{record && <></>}</RecordDetail>
}
