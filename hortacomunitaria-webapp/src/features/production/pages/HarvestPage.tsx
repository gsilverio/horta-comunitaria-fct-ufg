import { formatNumber } from "../../irrigation/formValues"
import { formatDate } from "../../../services/dates"
import { formatInstant } from "../../irrigation/formValues"
import { HarvestDestinations } from "../components/HarvestDestinations"
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
import type { Harvest } from "../types/Harvest"
import { harvestApi } from "../services/HarvestApi"
import { HarvestForm, HarvestReview } from "../components/HarvestForm"
import { enumLabels } from "../../../services/labels"
export function HarvestPage({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
if (route.id === "novo") return <HarvestForm garden={route.garden} saved={record => { saved("Registro salvo. Recarregando dados confirmados."); navigate(route.garden, route.module, route.resource, record.id, route.search) }} cancel={() => navigate(route.garden, route.module, route.resource, "", route.search)} />
if (route.id) return <HarvestDetail route={route} saved={saved} />
return <HarvestList route={route} />
}
function HarvestList({ route }: { route: RecordRoute }) {
const filters = useListFilters(route, ['from', 'to', 'validity', 'cycleId', 'speciesId'], true)
const options = useLookups(route.garden, ['cultivationCycle', 'species'])
const params = { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, cycleId: filters.values.cycleId || undefined, speciesId: filters.values.speciesId || undefined }
const queryKey = JSON.stringify(params)
const load = useCallback((signal: AbortSignal) => harvestApi.list(route.garden, { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, cycleId: filters.values.cycleId || undefined, speciesId: filters.values.speciesId || undefined }, signal), [filters.page, filters.values.from, filters.values.to, filters.values.validity, filters.values.cycleId, filters.values.speciesId, route.garden])
const result = useRemote(queryKey + "/" + filters.revision, load)
return <RecordList title="Colheitas" hint="Período da data do registro. Consulte a origem, corrija com motivo e preserve o histórico." create={() => navigate(route.garden, route.module, route.resource, "novo", route.search)} apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} data={result.data} page={filters.changePage} filters={<>
<Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change("from", value)} error={filters.error} />
<Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change("to", value)} error={filters.error} />
<SelectField name="validity" label="Validade" value={filters.draft.validity} change={value => filters.change("validity", value)} options={[{ id: 'Valid', label: enumLabels['Valid'] }, { id: 'Invalid', label: enumLabels['Invalid'] }]} />
<SelectField name="cycleId" label="Ciclo" value={filters.draft.cycleId} change={value => filters.change("cycleId", value)} options={options.data?.cultivationCycle ?? []} />
<SelectField name="speciesId" label="Espécie" value={filters.draft.speciesId} change={value => filters.change("speciesId", value)} options={options.data?.species ?? []} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}>
{result.data?.items.map(item => <li className="py-4" key={item.id}><a className="font-semibold underline decoration-horta-line underline-offset-4" href={routeHref(route.garden, route.module, route.resource, item.id, route.search)}>{formatDate(item.harvestedOn)}</a><span className="ml-3 text-sm text-horta-muted">{enumLabels[item.validity]} · v{item.version}</span><small className="record-description">Massa fresca: {formatNumber(item.freshMassKg)} kg · Aproveitável: {item.usableMassKg === null ? "Não informado" : `${formatNumber(item.usableMassKg)} kg`}</small></li>)}
</RecordList>
}
function HarvestDetail({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
const get = useCallback((signal: AbortSignal) => harvestApi.get(route.garden, route.id, signal), [route.id, route.garden])
const audit = useCallback((page: number, signal: AbortSignal) => harvestApi.history(route.garden, route.id, page, signal), [route.id, route.garden])
const state = useRecord<Harvest>(route.id, get, audit, (version, reason) => harvestApi.deactivate(route.garden, route.id, version, reason), () => saved("Alteração confirmada."))
const record = state.detail.data
if (route.search.get("edit") === "1" && record) return <HarvestForm garden={route.garden} original={record} saved={record => { saved("Registro salvo. Dados e indicadores serão recarregados."); const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, record.id, query); state.refresh() }} cancel={() => { const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, route.id, query) }} />
return <RecordDetail state={state} title="Colheita" version={record?.version ?? 0} active={record?.validity === "Valid"} catalog={false} back={() => { const query = new URLSearchParams(route.search); query.delete("edit"); navigate(route.garden, route.module, route.resource, "", query) }} edit={() => { const query = new URLSearchParams(route.search); query.set("edit", "1"); navigate(route.garden, route.module, route.resource, route.id, query) }} items={record ? [{ label: "Valores do registro", value: <HarvestReview record={record} /> }, { label: "Incluído em", value: formatInstant(record.recordedAt) }, { label: "Autor técnico (demonstração)", value: record.recordedByUserId }, { label: "Massa destinada (kg)", value: record.allocatedMassKg }, { label: "Saldo aproveitável (kg)", value: record.remainingUsableMassKg }] : []}>{record && <><HarvestDestinations garden={route.garden} harvest={record.id} />{record.validity === 'Valid' && <section className="next-step"><h3>Próxima etapa: secagem</h3>{record.usableMassKg === null ? <p>A massa aproveitável ainda não foi medida. Corrija a colheita depois da pesagem para permitir a destinação.</p> : record.remainingUsableMassKg !== null && record.remainingUsableMassKg > 0 ? <><p>Destine somente o saldo aproveitável confirmado. Depois, finalize o lote com a pesagem seca.</p><a className="action-link" href={routeHref(route.garden, 'producao', 'secagens', 'novo', new URLSearchParams({ harvestId: record.id }))}>Criar lote de secagem desta colheita</a></> : <p>Sem saldo aproveitável disponível para uma nova destinação.</p>}</section>}</>}</RecordDetail>
}

