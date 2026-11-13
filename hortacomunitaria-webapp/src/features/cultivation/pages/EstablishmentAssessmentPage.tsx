import type { AppRoute } from '../../../routes/routes'
import { goToSame } from '../../../routes/navigation'
import { linkTo, samePage } from '../../../routes/routes'
import { EvidenceSection } from "../../evidence/pages/EvidenceSection"
import { RecordRow } from '../../../components/RecordRow'
import { choiceLabel } from '../../../services/lookups'
import { formatNumber } from "../../irrigation/formValues"
import { formatDate } from "../../../services/dates"
import { formatInstant } from "../../irrigation/formValues"
import { useCallback } from "react"
import { RecordList } from "../../../components/RecordList"
import { RecordDetail } from "../../../components/RecordDetail"
import { Facts, Field, SelectField } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useRecord } from "../../../hooks/useRecord"
import { useRemote } from "../../../hooks/useRemote"
import { useLookups } from "../../../hooks/useLookups"
import type { LookupChoice, LookupKey } from "../../../services/lookups"
import { useListFilters } from "../../../hooks/useListFilters"

import type { EstablishmentAssessment } from "../types/EstablishmentAssessment"
import { establishmentAssessmentApi } from "../services/EstablishmentAssessmentApi"
import { EstablishmentAssessmentForm } from "../components/EstablishmentAssessmentForm"
import { enumLabels } from "../../../services/labels"
export function EstablishmentAssessmentPage({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
if (route.id === "novo") return <EstablishmentAssessmentForm garden={route.garden} saved={record => { saved("Registro salvo. Recarregando dados confirmados."); goToSame(route, record.id, route.search) }} cancel={() => goToSame(route, '', route.search)} />
if (route.id) return <EstablishmentAssessmentDetail route={route} saved={saved} />
return <EstablishmentAssessmentList route={route} />
}
function EstablishmentAssessmentList({ route }: { route: AppRoute }) {
const filters = useListFilters(route, ['from', 'to', 'validity', 'plantingId', 'method', 'cycleId', 'speciesId'], true)
const options = useLookups(route.garden, ['cultivationCycle', 'planting', 'species', 'bed'])
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
{result.data?.items.map(item => <RecordRow key={item.id} href={samePage(route, item.id, route.search)} title={plantingContext(options.data, item.plantingId).title} description={`${formatDate(item.assessedOn)} · ${formatNumber(item.survivingQuantity)} sobreviventes · ${item.method}`} status={item.validity === "Invalid" ? enumLabels[item.validity] : undefined} statusTone="invalid" />)}
</RecordList>
}
/** Espécie e canteiro do plantio avaliado, pelo ciclo; os rótulos técnicos ficam só nos seletores. */
function plantingContext(data: Partial<Record<LookupKey, LookupChoice[]>> | undefined, plantingId: string) {
const planting = data?.planting?.find(item => item.id === plantingId)
const cycle = planting && data?.cultivationCycle?.find(item => item.id === planting.cycleId)
const species = cycle ? choiceLabel(data?.species, cycle.speciesId) : undefined
const bed = cycle ? choiceLabel(data?.bed, cycle.bedId) : undefined
return { planting, cycle, species, bed, title: species && bed ? `${species} · ${bed}` : species ?? 'Plantio avaliado' }
}
function EstablishmentAssessmentDetail({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
const get = useCallback((signal: AbortSignal) => establishmentAssessmentApi.get(route.garden, route.id, signal), [route.id, route.garden])
const audit = useCallback((page: number, signal: AbortSignal) => establishmentAssessmentApi.history(route.garden, route.id, page, signal), [route.id, route.garden])
const state = useRecord<EstablishmentAssessment>(route.id, get, audit, (version, reason) => establishmentAssessmentApi.deactivate(route.garden, route.id, version, reason), () => saved("Alteração confirmada."))
const record = state.detail.data
const options = useLookups(route.garden, ['cultivationCycle', 'planting', 'species', 'bed', 'participant'])
if (route.action === 'editar' && record) return <EstablishmentAssessmentForm garden={route.garden} original={record} saved={record => { saved("Registro salvo. Dados e indicadores serão recarregados."); const query = new URLSearchParams(route.search); goToSame(route, record.id, query); state.refresh() }} cancel={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query) }} />
const context = record ? plantingContext(options.data, record.plantingId) : undefined
// Resumo no cabeçalho; os dados completos ficam numa grade única, sem identificadores técnicos.
const description = record && [context?.bed, `Avaliada em ${formatDate(record.assessedOn)}`, `${formatNumber(record.survivingQuantity)} sobreviventes`].filter(Boolean).join(' · ')
const facts = record && <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[
  { label: 'Plantio avaliado', value: <a className="underline" href={linkTo(route.garden, 'cultivo/plantios', record.plantingId)}>{context?.planting?.label ?? 'Plantio'}</a> },
  { label: 'Ciclo', value: context?.cycle ? <a className="underline" href={linkTo(route.garden, 'cultivo/ciclos', context.cycle.id)}>{context.cycle.label}</a> : 'Não informado' },
  { label: 'Data da avaliação', value: formatDate(record.assessedOn) },
  { label: 'Plantas sobreviventes', value: formatNumber(record.survivingQuantity) },
  { label: 'Método', value: record.method },
  { label: 'Responsável pelo trabalho', value: record.responsibleParticipantId ? <a className="underline" href={linkTo(route.garden, 'participantes', record.responsibleParticipantId)}>{choiceLabel(options.data?.participant, record.responsibleParticipantId) ?? 'Participante'}</a> : 'Não informado' },
  { label: 'Incluído em', value: formatInstant(record.recordedAt) },
  { label: 'Observações', value: record.notes || 'Não informadas' },
]} /></>
return <RecordDetail state={state} eyebrow="Avaliação de pegamento" title={context?.species ?? 'Avaliação de pegamento'} description={description} version={record?.version ?? 0} active={record?.validity === "Valid"} catalog={false}
  back={() => { const query = new URLSearchParams(route.search); goToSame(route, '', query) }} edit={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query, 'editar') }}
  items={facts ? [{ label: 'Dados da avaliação', value: facts }] : []}
  attachments={record && <EvidenceSection garden={route.garden} targetType="EstablishmentAssessment" targetId={record.id} active={record.validity === "Valid"} />} />
}
