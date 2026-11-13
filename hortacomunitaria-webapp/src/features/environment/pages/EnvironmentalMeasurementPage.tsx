import type { AppRoute } from '../../../routes/routes'
import { goToSame } from '../../../routes/navigation'
import { samePage } from '../../../routes/routes'
import { formatNumber } from "../../irrigation/formValues"
import { RecordRow } from '../../../components/RecordRow'
import { choiceLabel } from '../../../services/lookups'
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

import type { EnvironmentalMeasurement } from "../types/EnvironmentalMeasurement"
import { environmentalMeasurementApi } from "../services/EnvironmentalMeasurementApi"
import { EnvironmentalMeasurementForm, EnvironmentalMeasurementReview } from "../components/EnvironmentalMeasurementForm"
import { enumLabels } from "../../../services/labels"
export function EnvironmentalMeasurementPage({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
if (route.id === "novo") return <EnvironmentalMeasurementForm garden={route.garden} saved={record => { saved("Registro salvo. Recarregando dados confirmados."); goToSame(route, record.id, route.search) }} cancel={() => goToSame(route, '', route.search)} />
if (route.id) return <EnvironmentalMeasurementDetail route={route} saved={saved} />
return <EnvironmentalMeasurementList route={route} />
}
function EnvironmentalMeasurementList({ route }: { route: AppRoute }) {
const filters = useListFilters(route, ['from', 'to', 'validity', 'pointId', 'variableId', 'unit', 'method', 'quality'], true)
const options = useLookups(route.garden, ['environmentalVariable', 'samplingPoint'])
const params = { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, pointId: filters.values.pointId || undefined, variableId: filters.values.variableId || undefined, unit: filters.values.unit || undefined, method: filters.values.method || undefined, quality: filters.values.quality || undefined }
const queryKey = JSON.stringify(params)
const load = useCallback((signal: AbortSignal) => environmentalMeasurementApi.list(route.garden, { page: filters.page, pageSize: 10, from: filters.values.from || undefined, to: filters.values.to || undefined, validity: filters.values.validity || undefined, pointId: filters.values.pointId || undefined, variableId: filters.values.variableId || undefined, unit: filters.values.unit || undefined, method: filters.values.method || undefined, quality: filters.values.quality || undefined }, signal), [filters.page, filters.values.from, filters.values.to, filters.values.validity, filters.values.pointId, filters.values.variableId, filters.values.unit, filters.values.method, filters.values.quality, route.garden])
const result = useRemote(queryKey + "/" + filters.revision, load)
return <RecordList title="Medições ambientais" hint="Período da data do registro. Consulte a origem, corrija com motivo e preserve o histórico." create={() => goToSame(route, "novo", route.search)} apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} data={result.data} page={filters.changePage} filters={<>
<Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change("from", value)} error={filters.error} />
<Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change("to", value)} error={filters.error} />
<SelectField name="validity" label="Validade" value={filters.draft.validity} change={value => filters.change("validity", value)} options={[{ id: 'Valid', label: enumLabels['Valid'] }, { id: 'Invalid', label: enumLabels['Invalid'] }]} />
<SelectField name="pointId" label="Ponto de coleta" value={filters.draft.pointId} change={value => filters.change("pointId", value)} options={options.data?.samplingPoint ?? []} />
<SelectField name="variableId" label="Variável" value={filters.draft.variableId} change={value => filters.change("variableId", value)} options={options.data?.environmentalVariable ?? []} />
<Field name="unit" label="Unidade" type="text" value={filters.draft.unit} change={value => filters.change("unit", value)} />
<Field name="method" label="Método" type="text" value={filters.draft.method} change={value => filters.change("method", value)} />
<SelectField name="quality" label="Qualidade" value={filters.draft.quality} change={value => filters.change("quality", value)} options={[{ id: 'Normal', label: enumLabels['Normal'] }, { id: 'Questionable', label: enumLabels['Questionable'] }]} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}>
{result.data?.items.map(item => <RecordRow key={item.id} href={samePage(route, item.id, route.search)} title={choiceLabel(options.data?.environmentalVariable, item.variableId) ?? "Medição"} description={`${formatInstant(item.collectedAt)} · ${choiceLabel(options.data?.samplingPoint, item.samplingPointId) ?? "Ponto de coleta"}`} meta={<strong className="text-sm">{formatNumber(item.value)} {item.unit}</strong>} status={item.validity === "Invalid" ? enumLabels[item.validity] : undefined} statusTone="invalid" />)}
</RecordList>
}
function EnvironmentalMeasurementDetail({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
const get = useCallback((signal: AbortSignal) => environmentalMeasurementApi.get(route.garden, route.id, signal), [route.id, route.garden])
const audit = useCallback((page: number, signal: AbortSignal) => environmentalMeasurementApi.history(route.garden, route.id, page, signal), [route.id, route.garden])
const state = useRecord<EnvironmentalMeasurement>(route.id, get, audit, (version, reason) => environmentalMeasurementApi.deactivate(route.garden, route.id, version, reason), () => saved("Alteração confirmada."))
const record = state.detail.data
if (route.action === 'editar' && record) return <EnvironmentalMeasurementForm garden={route.garden} original={record} saved={record => { saved("Registro salvo. Dados e indicadores serão recarregados."); const query = new URLSearchParams(route.search); goToSame(route, record.id, query); state.refresh() }} cancel={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query) }} />
return <RecordDetail state={state} title="Medição ambiental" version={record?.version ?? 0} active={record?.validity === "Valid"} catalog={false} back={() => { const query = new URLSearchParams(route.search); goToSame(route, '', query) }} edit={() => { const query = new URLSearchParams(route.search); goToSame(route, route.id, query, 'editar') }} items={record ? [{ label: "Valores do registro", value: <EnvironmentalMeasurementReview record={record} /> }, { label: "Incluído em", value: formatInstant(record.recordedAt) }, { label: "Canteiro histórico", value: record.bedId }, { label: "Setor histórico", value: record.sectorId }, { label: "Associação preservada", value: record.bedSectorAssignmentId }] : []}>{record && <></>}</RecordDetail>
}
