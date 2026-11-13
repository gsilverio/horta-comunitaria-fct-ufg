import type { AppRoute } from '../../../routes/routes'
import { goToSame } from '../../../routes/navigation'
import { linkTo, samePage } from '../../../routes/routes'
import { EvidenceSection } from "../../evidence/pages/EvidenceSection"
import { RecordRow } from '../../../components/RecordRow'
import { choiceLabel } from '../../../services/lookups'
import { formatNumber } from "../../irrigation/formValues"
import { formatDate } from "../../../services/dates"
import { formatInstant } from "../../irrigation/formValues"
import { HarvestDestinations } from "../components/HarvestDestinations"
import { useCallback } from "react"
import { RecordList } from "../../../components/RecordList"
import { RecordDetail } from "../../../components/RecordDetail"
import { Facts, Field, SelectField } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useRecord } from "../../../hooks/useRecord"
import { useRemote } from "../../../hooks/useRemote"
import { useLookups } from "../../../hooks/useLookups"
import { useListFilters } from "../../../hooks/useListFilters"

import type { Harvest } from "../types/Harvest"
import { harvestApi } from "../services/HarvestApi"
import { HarvestForm } from "../components/HarvestForm"
import { enumLabels } from "../../../services/labels"
export function HarvestPage({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
if (route.id === "novo") return <HarvestForm garden={route.garden} saved={record => { saved("Registro salvo. Recarregando dados confirmados."); goToSame(route, record.id, route.search) }} cancel={() => goToSame(route, '', route.search)} />
if (route.id) return <HarvestDetail route={route} saved={saved} />
return <HarvestList route={route} />
}
function HarvestList({ route }: { route: AppRoute }) {
const filters = useListFilters(route, ['from', 'to', 'validity', 'cycleId', 'speciesId'], true)
const options = useLookups(route.garden, ['cultivationCycle', 'species'])
const params = { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, cycleId: filters.values.cycleId || undefined, speciesId: filters.values.speciesId || undefined }
const queryKey = JSON.stringify(params)
const load = useCallback((signal: AbortSignal) => harvestApi.list(route.garden, { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, cycleId: filters.values.cycleId || undefined, speciesId: filters.values.speciesId || undefined }, signal), [filters.page, filters.values.from, filters.values.to, filters.values.validity, filters.values.cycleId, filters.values.speciesId, route.garden])
const result = useRemote(queryKey + "/" + filters.revision, load)
return <RecordList title="Colheitas" hint="Período da data do registro. Consulte a origem, corrija com motivo e preserve o histórico." create={() => goToSame(route, "novo", route.search)} apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} data={result.data} page={filters.changePage} filters={<>
<Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change("from", value)} error={filters.error} />
<Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change("to", value)} error={filters.error} />
<SelectField name="validity" label="Validade" value={filters.draft.validity} change={value => filters.change("validity", value)} options={[{ id: 'Valid', label: enumLabels['Valid'] }, { id: 'Invalid', label: enumLabels['Invalid'] }]} />
<SelectField name="cycleId" label="Ciclo" value={filters.draft.cycleId} change={value => filters.change("cycleId", value)} options={options.data?.cultivationCycle ?? []} />
<SelectField name="speciesId" label="Espécie" value={filters.draft.speciesId} change={value => filters.change("speciesId", value)} options={options.data?.species ?? []} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}>
{result.data?.items.map(item => <RecordRow key={item.id} href={samePage(route, item.id, route.search)} title={choiceLabel(options.data?.cultivationCycle, item.cycleId) ?? "Ciclo"} description={`${formatDate(item.harvestedOn)} · Aproveitável: ${item.usableMassKg === null ? "Não informado" : `${formatNumber(item.usableMassKg)} kg`}`} meta={<strong className="text-sm">{formatNumber(item.freshMassKg)} kg frescos</strong>} status={item.validity === "Invalid" ? enumLabels[item.validity] : undefined} statusTone="invalid" />)}
</RecordList>
}
function HarvestDetail({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
const get = useCallback((signal: AbortSignal) => harvestApi.get(route.garden, route.id, signal), [route.id, route.garden])
const audit = useCallback((page: number, signal: AbortSignal) => harvestApi.history(route.garden, route.id, page, signal), [route.id, route.garden])
const state = useRecord<Harvest>(route.id, get, audit, (version, reason) => harvestApi.deactivate(route.garden, route.id, version, reason), () => saved("Alteração confirmada."))
const record = state.detail.data
const options = useLookups(route.garden, ['cultivationCycle', 'species', 'bed', 'participant'])
if (route.action === 'editar' && record) return <HarvestForm garden={route.garden} original={record} saved={record => { saved("Registro salvo. Dados e indicadores serão recarregados."); const query = new URLSearchParams(route.search); goToSame(route, record.id, query); state.refresh() }} cancel={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query) }} />
const cycle = record && options.data?.cultivationCycle?.find(item => item.id === record.cycleId)
const speciesName = choiceLabel(options.data?.species, cycle?.speciesId), bedName = choiceLabel(options.data?.bed, cycle?.bedId)
const mass = (value: number | null, empty: string) => value === null ? empty : `${formatNumber(value)} kg`
// Resumo no cabeçalho; os dados completos ficam numa grade única, sem identificadores técnicos.
const description = record && [bedName, formatDate(record.harvestedOn), `${formatNumber(record.freshMassKg)} kg colhidos`].filter(Boolean).join(' · ')
const facts = record && <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[
  { label: 'Ciclo', value: <a className="underline" href={linkTo(route.garden, 'cultivo/ciclos', record.cycleId)}>{cycle?.label ?? 'Ciclo de cultivo'}</a> },
  { label: 'Data da colheita', value: formatDate(record.harvestedOn) },
  { label: 'Massa fresca', value: mass(record.freshMassKg, 'Não informada') },
  { label: 'Massa aproveitável', value: mass(record.usableMassKg, 'Não medida') },
  { label: 'Massa destinada à secagem', value: mass(record.allocatedMassKg, 'Nenhuma') },
  { label: 'Saldo aproveitável', value: mass(record.remainingUsableMassKg, 'Não calculado') },
  { label: 'Motivo da perda', value: record.lossReason || 'Não informado' },
  { label: 'Responsável pelo trabalho', value: record.responsibleParticipantId ? <a className="underline" href={linkTo(route.garden, 'participantes', record.responsibleParticipantId)}>{choiceLabel(options.data?.participant, record.responsibleParticipantId) ?? 'Participante'}</a> : 'Não informado' },
  { label: 'Incluído em', value: formatInstant(record.recordedAt) },
]} /></>
const nextStep = record && record.validity === 'Valid' && <section className="next-step"><h3>Próxima etapa: secagem</h3>{record.usableMassKg === null ? <p>A massa aproveitável ainda não foi medida. Corrija a colheita depois da pesagem para permitir a destinação.</p> : record.remainingUsableMassKg !== null && record.remainingUsableMassKg > 0 ? <><p>Destine somente o saldo aproveitável confirmado. Depois, finalize o lote com a pesagem seca.</p><a className="action-link" href={linkTo(route.garden, 'producao/secagens', 'novo', new URLSearchParams({ harvestId: record.id }))}>Criar lote de secagem</a></> : <p>Sem saldo aproveitável disponível para uma nova destinação.</p>}</section>
return <RecordDetail state={state} eyebrow="Colheita" title={speciesName ?? 'Colheita'} description={description} version={record?.version ?? 0} active={record?.validity === "Valid"} catalog={false}
  back={() => { const query = new URLSearchParams(route.search); goToSame(route, '', query) }} edit={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query, 'editar') }}
  sections={record ? [{ id: 'secagem', label: 'Secagem', content: <div className="panel"><div className="cycle-records">{nextStep}<HarvestDestinations garden={route.garden} harvest={record.id} /></div></div> }] : []}
  items={facts ? [{ label: 'Dados da colheita', value: facts }] : []}
  attachments={record && <EvidenceSection garden={route.garden} targetType="Harvest" targetId={record.id} active={record.validity === "Valid"} />} />
}

