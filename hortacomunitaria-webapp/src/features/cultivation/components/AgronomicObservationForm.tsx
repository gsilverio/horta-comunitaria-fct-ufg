import { routeHref } from "../../../hooks/useRoute"
import { readRoute } from "../../../hooks/useRoute"
import { useState } from "react"
import { EditorFrame } from "../../../components/EditorFrame"
import { Field, SelectField, Facts } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useEditor } from "../../../hooks/useEditor"
import { useLookups } from "../../../hooks/useLookups"
import { agronomicObservationApi } from "../services/AgronomicObservationApi"
import type { AgronomicObservation, AgronomicObservationValues } from "../types/AgronomicObservation"
import { today, formatNumber } from "../../irrigation/formValues"
import { numberDraft, numeric, optionalText } from "../../../services/values"
export function AgronomicObservationForm({ garden, original, saved, cancel }: { garden: string; original?: AgronomicObservation; saved: (record: AgronomicObservation) => void; cancel: () => void }) {
const editor = useEditor<AgronomicObservation, AgronomicObservationValues>(original, (values, key) => agronomicObservationApi.create(garden, values, key), (values, version, reason) => { if (!original) throw new Error("Registro indisponível"); return agronomicObservationApi.correct(garden, original.id, values, version, reason) }, () => { if (!original) throw new Error("Registro indisponível"); return agronomicObservationApi.get(garden, original.id) }, saved, true)
const [draft, setDraft] = useState<{ cycleId: string; observedOn: string; category: string; description: string; measurement: string; unit: string; method: string; responsibleParticipantId: string }>(() => ({
cycleId: original ? original.cycleId : readRoute().search.get('cycleId') || '',
observedOn: original ? original.observedOn : today(),
category: original ? original.category : '',
description: original ? original.description : '',
measurement: original ? numberDraft(original.measurement) : '',
unit: original ? original.unit ?? "" : '',
method: original ? original.method ?? "" : '',
responsibleParticipantId: original ? original.responsibleParticipantId ?? "" : ''
}))
const options = useLookups(garden, ['cultivationCycle', 'participant'])
function change<K extends keyof typeof draft>(key: K, value: typeof draft[K]) { setDraft(current => ({ ...current, [key]: value })) }
function values(): AgronomicObservationValues { if (options.loading || options.error) throw new Error("Aguarde ou recarregue os cadastros de apoio."); return {
cycleId: draft.cycleId,
observedOn: draft.observedOn,
category: draft.category.trim(),
description: draft.description.trim(),
measurement: numeric(draft.measurement, 'measurement', false, 3, true),
unit: optionalText(draft.unit),
method: optionalText(draft.method),
responsibleParticipantId: optionalText(draft.responsibleParticipantId),
} }
return <EditorFrame dependenciesReady={!options.loading && !options.error} title={original ? "Corrigir: Observação agronômica" : "Novo: Observação agronômica"} busy={editor.busy} uncertain={!!editor.attempt || editor.catalogUncertain} catalogUncertain={editor.catalogUncertain} error={editor.error} reason={original ? editor.reason : undefined} changeReason={original ? editor.setReason : undefined} submit={() => void editor.submit(values)} cancel={cancel} conflict={original ? () => void editor.reloadConflict() : undefined} review={editor.review && <Facts items={[{ label: "Versão", value: editor.review.version }, { label: "Valores atuais", value: <AgronomicObservationReview record={editor.review} /> }]} />} acceptReview={editor.acceptReview}>
{options.loading && <p role="status">Carregando cadastros de apoio…</p>}{options.error && <Failure error={options.error} retry={options.retry} />}
<div className="grid min-w-0 gap-4 sm:grid-cols-2">
<SelectField name="cycleId" label="Ciclo" required value={draft.cycleId} change={value => change("cycleId", value)} options={options.data?.cultivationCycle ?? []} error={editor.error} />
<Field name="observedOn" label="Data da observação" required type="date" value={draft.observedOn} change={value => change("observedOn", value)} error={editor.error} />
<Field name="category" label="Categoria" required type="text" value={draft.category} change={value => change("category", value)} error={editor.error} />
<Field name="description" label="Descrição" required type="textarea" value={draft.description} change={value => change("description", value)} error={editor.error} />
<Field name="measurement" label="Medida" type="decimal" value={draft.measurement} change={value => change("measurement", value)} error={editor.error} />
<Field name="unit" label="Unidade" type="text" value={draft.unit} change={value => change("unit", value)} error={editor.error} />
<Field name="method" label="Método" type="text" value={draft.method} change={value => change("method", value)} error={editor.error} />
<SelectField name="responsibleParticipantId" label="Responsável pelo trabalho" value={draft.responsibleParticipantId} change={value => change("responsibleParticipantId", value)} options={options.data?.participant ?? []} error={editor.error} />
</div></EditorFrame>
}
export function AgronomicObservationReview({ record }: { record: AgronomicObservation }) { const options = useLookups(record.gardenId, ['cultivationCycle', 'participant']); return <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[{ label: 'Ciclo', value: record.cycleId ? <a className="underline" href={routeHref(readRoute().garden, 'cultivo', 'ciclos', record.cycleId)}>{options.data?.cultivationCycle?.find(x => x.id === record.cycleId)?.label ?? record.cycleId}</a> : "Não informado" }, { label: 'Data da observação', value: record.observedOn ? record.observedOn.split("-").reverse().join("/") : "Não informado" }, { label: 'Categoria', value: record.category }, { label: 'Descrição', value: record.description }, { label: 'Medida', value: record.measurement === null ? "Não informado" : formatNumber(record.measurement) }, { label: 'Unidade', value: record.unit }, { label: 'Método', value: record.method }, { label: 'Responsável pelo trabalho', value: record.responsibleParticipantId ? <a className="underline" href={routeHref(readRoute().garden, 'atividades', 'participantes', record.responsibleParticipantId)}>{options.data?.participant?.find(x => x.id === record.responsibleParticipantId)?.label ?? record.responsibleParticipantId}</a> : "Não informado" }]} /></> }
