import type { AppRoute } from '../../../routes/routes'
import { goToSame } from '../../../routes/navigation'
import { samePage } from '../../../routes/routes'
import { EvidenceSection } from "../../evidence/pages/EvidenceSection"
import { RecordRow } from '../../../components/RecordRow'
import { choiceLabel } from '../../../services/lookups'
import { formatNumber } from "../../irrigation/formValues"
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

import type { EstablishmentAssessment } from "../types/EstablishmentAssessment"
import { establishmentAssessmentApi } from "../services/EstablishmentAssessmentApi"
import { EstablishmentAssessmentForm, EstablishmentAssessmentReview } from "../components/EstablishmentAssessmentForm"
import { enumLabels } from "../../../services/labels"
export function EstablishmentAssessmentPage({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
if (route.id === "novo") return <EstablishmentAssessmentForm garden={route.garden} saved={record => { saved("Registro salvo. Recarregando dados confirmados."); goToSame(route, record.id, route.search) }} cancel={() => goToSame(route, '', route.search)} />
if (route.id) return <EstablishmentAssessmentDetail route={route} saved={saved} />
return <EstablishmentAssessmentList route={route} />
}
function EstablishmentAssessmentList({ route }: { route: AppRoute }) {
const filters = useListFilters(route, ['from', 'to', 'validity', 'plantingId', 'method', 'cycleId', 'speciesId'], true)
const options = useLookups(route.garden, ['cultivationCycle', 'planting', 'species'])
const params = { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, plantingId: filters.values.plantingId || undefined, method: filters.values.method || undefined, cycleId: filters.values.cycleId || undefined, speciesId: filters.values.speciesId || undefined }
const queryKey = JSON.stringify(params)
const load = useCallback((signal: AbortSignal) => establishmentAssessmentApi.list(route.garden, { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, plantingId: filters.values.plantingId || undefined, method: filters.values.method || undefined, cycleId: filters.values.cycleId || undefined, speciesId: filters.values.speciesId || undefined }, signal), [filters.page, filters.values.from, filters.values.to, filters.values.validity, filters.values.plantingId, filters.values.method, filters.values.cycleId, filters.values.speciesId, route.garden])
const result = useRemote(queryKey + "/" + filters.revision, load)
return <RecordList title="Avaliações de pegamento" hint="Período da data do registro. Consulte a origem, corrija com motivo e preserve o histórico." create={() => goToSame(route, "novo", route.search)} apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} data={result.data} page={filters.changePage} filters={<>
<Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change("from", value)} error={filters.error} />
<Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change("to", value)} error={filters.error} />
<SelectField name="validity" label="Validade" value={filters.draft.validity} change={value => filters.change("validity", value)} options={[{ id: 'Valid', label: enumLabels['Valid'] }, { id: 'Invalid', label: enumLabels['Invalid'] }]} />
<SelectField name="plantingId" label="Plantio avaliado" value={filters.draft.plantingId} change={value => filters.change("plantingId", value)} options={options.data?.planting ?? []} />
<Field name="method" label="Método" type="text" value={filters.draft.method} change={value => filters.change("method", value)} />
<SelectField name="cycleId" label="Ciclo" value={filters.draft.cycleId} change={value => filters.change("cycleId", value)} options={options.data?.cultivationCycle ?? []} />
<SelectField name="speciesId" label="Espécie" value={filters.draft.speciesId} change={value => filters.change("speciesId", value)} options={options.data?.species ?? []} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}>
{result.data?.items.map(item => <RecordRow key={item.id} href={samePage(route, item.id, route.search)} title={choiceLabel(options.data?.planting, item.plantingId) ?? "Plantio"} description={`${formatDate(item.assessedOn)} · ${formatNumber(item.survivingQuantity)} sobreviventes · ${item.method}`} status={item.validity === "Invalid" ? enumLabels[item.validity] : undefined} statusTone="invalid" />)}
</RecordList>
}
function EstablishmentAssessmentDetail({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
const get = useCallback((signal: AbortSignal) => establishmentAssessmentApi.get(route.garden, route.id, signal), [route.id, route.garden])
const audit = useCallback((page: number, signal: AbortSignal) => establishmentAssessmentApi.history(route.garden, route.id, page, signal), [route.id, route.garden])
const state = useRecord<EstablishmentAssessment>(route.id, get, audit, (version, reason) => establishmentAssessmentApi.deactivate(route.garden, route.id, version, reason), () => saved("Alteração confirmada."))
const record = state.detail.data
if (route.action === 'editar' && record) return <EstablishmentAssessmentForm garden={route.garden} original={record} saved={record => { saved("Registro salvo. Dados e indicadores serão recarregados."); const query = new URLSearchParams(route.search); goToSame(route, record.id, query); state.refresh() }} cancel={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query) }} />
return <RecordDetail attachments={record && <EvidenceSection garden={route.garden} targetType="EstablishmentAssessment" targetId={record.id} active={record.validity === "Valid"} />} state={state} title="Avaliação de pegamento" version={record?.version ?? 0} active={record?.validity === "Valid"} catalog={false} back={() => { const query = new URLSearchParams(route.search); goToSame(route, '', query) }} edit={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query, 'editar') }} items={record ? [{ label: "Valores do registro", value: <EstablishmentAssessmentReview record={record} /> }, { label: "Incluído em", value: formatInstant(record.recordedAt) }, { label: "Autor do registro (identificador)", value: record.recordedByUserId }] : []}>{record && <></>}</RecordDetail>
}
