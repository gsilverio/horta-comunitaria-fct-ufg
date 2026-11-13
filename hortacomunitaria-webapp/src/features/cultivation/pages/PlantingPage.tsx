import type { AppRoute } from '../../../routes/routes'
import { goToSame } from '../../../routes/navigation'
import { linkTo, samePage } from '../../../routes/routes'
import { EvidenceSection } from "../../evidence/pages/EvidenceSection"
import { RecordRow } from '../../../components/RecordRow'
import { choiceLabel } from '../../../services/lookups'
import { formatNumber } from "../../irrigation/formValues"
import { formatDate } from "../../../services/dates"
import { formatInstant } from "../../irrigation/formValues"
import { PlantingAssessments } from "../components/PlantingAssessments"
import { useCallback } from "react"
import { RecordList } from "../../../components/RecordList"
import { RecordDetail } from "../../../components/RecordDetail"
import { Facts, Field, SelectField } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useRecord } from "../../../hooks/useRecord"
import { useRemote } from "../../../hooks/useRemote"
import { useLookups } from "../../../hooks/useLookups"
import { useListFilters } from "../../../hooks/useListFilters"

import type { Planting } from "../types/Planting"
import { plantingApi } from "../services/PlantingApi"
import { PlantingForm } from "../components/PlantingForm"
import { enumLabels } from "../../../services/labels"
export function PlantingPage({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
if (route.id === "novo") return <PlantingForm garden={route.garden} saved={record => { saved("Registro salvo. Recarregando dados confirmados."); goToSame(route, record.id, route.search) }} cancel={() => goToSame(route, '', route.search)} />
if (route.id) return <PlantingDetail route={route} saved={saved} />
return <PlantingList route={route} />
}
function PlantingList({ route }: { route: AppRoute }) {
const filters = useListFilters(route, ['from', 'to', 'validity', 'cycleId', 'kind', 'bedId', 'speciesId'], true)
const options = useLookups(route.garden, ['bed', 'cultivationCycle', 'species'])
const params = { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, cycleId: filters.values.cycleId || undefined, kind: filters.values.kind || undefined, bedId: filters.values.bedId || undefined, speciesId: filters.values.speciesId || undefined }
const queryKey = JSON.stringify(params)
const load = useCallback((signal: AbortSignal) => plantingApi.list(route.garden, { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, cycleId: filters.values.cycleId || undefined, kind: filters.values.kind || undefined, bedId: filters.values.bedId || undefined, speciesId: filters.values.speciesId || undefined }, signal), [filters.page, filters.values.from, filters.values.to, filters.values.validity, filters.values.cycleId, filters.values.kind, filters.values.bedId, filters.values.speciesId, route.garden])
const result = useRemote(queryKey + "/" + filters.revision, load)
return <RecordList title="Plantios e reposições" hint="Período da data do registro. Consulte a origem, corrija com motivo e preserve o histórico." create={() => goToSame(route, "novo", route.search)} apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} data={result.data} page={filters.changePage} filters={<>
<Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change("from", value)} error={filters.error} />
<Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change("to", value)} error={filters.error} />
<SelectField name="validity" label="Validade" value={filters.draft.validity} change={value => filters.change("validity", value)} options={[{ id: 'Valid', label: enumLabels['Valid'] }, { id: 'Invalid', label: enumLabels['Invalid'] }]} />
<SelectField name="cycleId" label="Ciclo" value={filters.draft.cycleId} change={value => filters.change("cycleId", value)} options={options.data?.cultivationCycle ?? []} />
<SelectField name="kind" label="Natureza do plantio" value={filters.draft.kind} change={value => filters.change("kind", value)} options={[{ id: 'Initial', label: enumLabels['Initial'] }, { id: 'Replacement', label: enumLabels['Replacement'] }]} />
<SelectField name="bedId" label="Canteiro" value={filters.draft.bedId} change={value => filters.change("bedId", value)} options={options.data?.bed ?? []} />
<SelectField name="speciesId" label="Espécie" value={filters.draft.speciesId} change={value => filters.change("speciesId", value)} options={options.data?.species ?? []} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}>
{result.data?.items.map(item => <RecordRow key={item.id} href={samePage(route, item.id, route.search)} title={choiceLabel(options.data?.cultivationCycle, item.cycleId) ?? "Ciclo"} description={`${formatDate(item.plantedOn)} · ${enumLabels[item.kind]} · ${formatNumber(item.plantedQuantity)} plantas`} status={item.validity === "Invalid" ? enumLabels[item.validity] : undefined} statusTone="invalid" />)}
</RecordList>
}
function PlantingDetail({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
const get = useCallback((signal: AbortSignal) => plantingApi.get(route.garden, route.id, signal), [route.id, route.garden])
const audit = useCallback((page: number, signal: AbortSignal) => plantingApi.history(route.garden, route.id, page, signal), [route.id, route.garden])
const state = useRecord<Planting>(route.id, get, audit, (version, reason) => plantingApi.deactivate(route.garden, route.id, version, reason), () => saved("Alteração confirmada."))
const record = state.detail.data
const options = useLookups(route.garden, ['cultivationCycle', 'species', 'bed', 'participant', 'planting', 'propagationLot'])
if (route.action === 'editar' && record) return <PlantingForm garden={route.garden} original={record} saved={record => { saved("Registro salvo. Dados e indicadores serão recarregados."); const query = new URLSearchParams(route.search); goToSame(route, record.id, query); state.refresh() }} cancel={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query) }} />
const cycle = record && options.data?.cultivationCycle?.find(item => item.id === record.cycleId)
const speciesName = choiceLabel(options.data?.species, cycle?.speciesId)
const bedName = choiceLabel(options.data?.bed, cycle?.bedId)
const optional = (value: number | null, unit: string) => value === null ? 'Não informado' : `${formatNumber(value)} ${unit}`
// Resumo no cabeçalho; os dados completos ficam numa grade única, sem identificadores técnicos.
const description = record && [bedName, formatDate(record.plantedOn), `${formatNumber(record.plantedQuantity)} plantas`].filter(Boolean).join(' · ')
const facts = record && <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[
  { label: 'Ciclo', value: <a className="underline" href={linkTo(route.garden, 'cultivo/ciclos', record.cycleId)}>{cycle?.label ?? 'Ciclo de cultivo'}</a> },
  { label: 'Natureza do plantio', value: enumLabels[record.kind] },
  ...(record.kind === 'Replacement' ? [{ label: 'Plantio reposto', value: record.replacedPlantingId ? <a className="underline" href={linkTo(route.garden, 'cultivo/plantios', record.replacedPlantingId)}>{options.data?.planting?.find(item => item.id === record.replacedPlantingId)?.label ?? 'Plantio original'}</a> : 'Não informado' }] : []),
  { label: 'Data do plantio', value: formatDate(record.plantedOn) },
  { label: 'Quantidade plantada', value: `${formatNumber(record.plantedQuantity)} plantas` },
  { label: 'Lote de origem', value: record.propagationLotId ? <a className="underline" href={linkTo(route.garden, 'cultivo/origens', record.propagationLotId)}>{choiceLabel(options.data?.propagationLot, record.propagationLotId) ?? 'Lote de origem'}</a> : 'Não informado' },
  { label: 'Espaçamento entre plantas', value: optional(record.plantSpacingCm, 'cm') },
  { label: 'Espaçamento entre linhas', value: optional(record.rowSpacingCm, 'cm') },
  { label: 'Responsável pelo trabalho', value: record.responsibleParticipantId ? <a className="underline" href={linkTo(route.garden, 'participantes', record.responsibleParticipantId)}>{choiceLabel(options.data?.participant, record.responsibleParticipantId) ?? 'Participante'}</a> : 'Não informado' },
  { label: 'Incluído em', value: formatInstant(record.recordedAt) },
]} /></>
return <RecordDetail state={state} eyebrow={record ? (record.kind === 'Replacement' ? 'Reposição' : 'Plantio inicial') : 'Plantio'} title={speciesName ?? 'Plantio'} description={description} version={record?.version ?? 0} active={record?.validity === "Valid"} catalog={false}
  back={() => { const query = new URLSearchParams(route.search); goToSame(route, '', query) }} edit={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query, 'editar') }}
  sections={record ? [{ id: 'avaliacoes', label: 'Avaliações', content: <div className="panel"><PlantingAssessments garden={route.garden} record={record} /></div> }] : []}
  items={facts ? [{ label: 'Dados do plantio', value: facts }] : []}
  attachments={record && <EvidenceSection garden={route.garden} targetType="Planting" targetId={record.id} active={record.validity === "Valid"} />} />
}

