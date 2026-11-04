import { readRoute } from "../../../hooks/useRoute"
import { routeHref } from "../../../hooks/useRoute"
import { useState } from "react"
import { EditorFrame } from "../../../components/EditorFrame"
import { Field, SelectField, Facts } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useEditor } from "../../../hooks/useEditor"
import { useLookups } from "../../../hooks/useLookups"
import { environmentalMeasurementApi } from "../services/EnvironmentalMeasurementApi"
import type { EnvironmentalMeasurement, EnvironmentalMeasurementValues } from "../types/EnvironmentalMeasurement"
import { today, localInstant, formatNumber, formatInstant } from "../../irrigation/formValues"
import { numberDraft, requiredNumber, optionalText, instant } from "../../../services/values"
import { enumValue } from "../../../types/enums"
import type { MeasurementQuality } from "../../../types/enums"
import { enumLabels } from "../../../services/labels"
export function EnvironmentalMeasurementForm({ garden, original, saved, cancel }: { garden: string; original?: EnvironmentalMeasurement; saved: (record: EnvironmentalMeasurement) => void; cancel: () => void }) {
const editor = useEditor<EnvironmentalMeasurement, EnvironmentalMeasurementValues>(original, (values, key) => environmentalMeasurementApi.create(garden, values, key), (values, version, reason) => { if (!original) throw new Error("Registro indisponível"); return environmentalMeasurementApi.correct(garden, original.id, values, version, reason) }, () => { if (!original) throw new Error("Registro indisponível"); return environmentalMeasurementApi.get(garden, original.id) }, saved, true)
const [draft, setDraft] = useState<{ samplingPointId: string; variableId: string; collectedAt: string; value: string; unit: string; method: string; quality: string; responsibleParticipantId: string; notes: string }>(() => ({
samplingPointId: original ? original.samplingPointId : '',
variableId: original ? original.variableId : '',
collectedAt: original ? (original.collectedAt ? localInstant(original.collectedAt) : "") : today() + "T08:00",
value: original ? numberDraft(original.value) : '',
unit: original ? original.unit : '',
method: original ? original.method : '',
quality: original ? original.quality : 'Normal',
responsibleParticipantId: original ? original.responsibleParticipantId ?? "" : '',
notes: original ? original.notes ?? "" : ''
}))
const options = useLookups(garden, ['environmentalVariable', 'participant', 'samplingPoint'])
function change<K extends keyof typeof draft>(key: K, value: typeof draft[K]) { setDraft(current => ({ ...current, [key]: value, ...(key === 'variableId' ? { unit: options.data?.environmentalVariable?.find(x => x.id === value)?.referenceUnit ?? '' } : {}) })) }
function values(): EnvironmentalMeasurementValues { if (options.loading || options.error) throw new Error("Aguarde ou recarregue os cadastros de apoio."); return {
samplingPointId: draft.samplingPointId,
variableId: draft.variableId,
collectedAt: instant(draft.collectedAt, 'collectedAt', original?.collectedAt),
value: requiredNumber(draft.value, 'value', 3, true),
unit: draft.unit.trim(),
method: draft.method.trim(),
quality: enumValue<MeasurementQuality>(draft.quality, ['Normal', 'Questionable']),
responsibleParticipantId: optionalText(draft.responsibleParticipantId),
notes: optionalText(draft.notes),
} }
return <EditorFrame dependenciesReady={!options.loading && !options.error} title={original ? "Corrigir: Medição ambiental" : "Novo: Medição ambiental"} busy={editor.busy} uncertain={!!editor.attempt || editor.catalogUncertain} catalogUncertain={editor.catalogUncertain} error={editor.error} reason={original ? editor.reason : undefined} changeReason={original ? editor.setReason : undefined} submit={() => void editor.submit(values)} cancel={cancel} conflict={original ? () => void editor.reloadConflict() : undefined} review={editor.review && <Facts items={[{ label: "Versão", value: editor.review.version }, { label: "Valores atuais", value: <EnvironmentalMeasurementReview record={editor.review} /> }]} />} acceptReview={editor.acceptReview}>
{options.loading && <p role="status">Carregando cadastros de apoio…</p>}{options.error && <Failure error={options.error} retry={options.retry} />}
<div className="grid min-w-0 gap-4 sm:grid-cols-2">
<SelectField name="samplingPointId" label="Ponto de coleta" required value={draft.samplingPointId} change={value => change("samplingPointId", value)} options={options.data?.samplingPoint ?? []} error={editor.error} />
<SelectField name="variableId" label="Variável" required value={draft.variableId} change={value => change("variableId", value)} options={options.data?.environmentalVariable ?? []} error={editor.error} />
<Field name="collectedAt" label="Coletado em" required type="datetime-local" value={draft.collectedAt} change={value => change("collectedAt", value)} error={editor.error} />
<Field name="value" label="Valor medido" required type="decimal" value={draft.value} change={value => change("value", value)} error={editor.error} />
<Field name="unit" label="Unidade" required type="text" value={draft.unit} change={value => change("unit", value)} error={editor.error} />
<Field name="method" label="Método" required type="text" value={draft.method} change={value => change("method", value)} error={editor.error} />
<SelectField name="quality" label="Qualidade" required value={draft.quality} change={value => change("quality", value)} options={[{ id: 'Normal', label: enumLabels['Normal'] }, { id: 'Questionable', label: enumLabels['Questionable'] }]} error={editor.error} />
<SelectField name="responsibleParticipantId" label="Responsável pelo trabalho" value={draft.responsibleParticipantId} change={value => change("responsibleParticipantId", value)} options={options.data?.participant ?? []} error={editor.error} />
<Field name="notes" label="Observações" type="textarea" value={draft.notes} change={value => change("notes", value)} error={editor.error} />
</div></EditorFrame>
}
export function EnvironmentalMeasurementReview({ record }: { record: EnvironmentalMeasurement }) { const options = useLookups(record.gardenId, ['environmentalVariable', 'participant', 'samplingPoint']); return <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[{ label: 'Ponto de coleta', value: record.samplingPointId ? <a className="underline" href={routeHref(readRoute().garden, 'ambiente', 'pontos', record.samplingPointId)}>{options.data?.samplingPoint?.find(x => x.id === record.samplingPointId)?.label ?? record.samplingPointId}</a> : "Não informado" }, { label: 'Variável', value: record.variableId ? <a className="underline" href={routeHref(readRoute().garden, 'ambiente', 'variaveis', record.variableId)}>{options.data?.environmentalVariable?.find(x => x.id === record.variableId)?.label ?? record.variableId}</a> : "Não informado" }, { label: 'Coletado em', value: record.collectedAt ? formatInstant(record.collectedAt) : "Não informado" }, { label: 'Valor medido', value: record.value === null ? "Não informado" : formatNumber(record.value) }, { label: 'Unidade', value: record.unit }, { label: 'Método', value: record.method }, { label: 'Qualidade', value: enumLabels[record.quality] }, { label: 'Responsável pelo trabalho', value: record.responsibleParticipantId ? <a className="underline" href={routeHref(readRoute().garden, 'atividades', 'participantes', record.responsibleParticipantId)}>{options.data?.participant?.find(x => x.id === record.responsibleParticipantId)?.label ?? record.responsibleParticipantId}</a> : "Não informado" }, { label: 'Observações', value: record.notes }]} /></> }
