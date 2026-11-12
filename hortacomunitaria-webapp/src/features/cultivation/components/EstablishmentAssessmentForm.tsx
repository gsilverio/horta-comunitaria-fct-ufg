import { readRoute } from '../../../routes/navigation'
import { linkTo } from '../../../routes/routes'
import { useState } from "react"
import { EditorFrame } from "../../../components/EditorFrame"
import { Field, SelectField, Facts, Group } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useEditor } from "../../../hooks/useEditor"
import { useLookups } from "../../../hooks/useLookups"
import { establishmentAssessmentApi } from "../services/EstablishmentAssessmentApi"
import type { EstablishmentAssessment, EstablishmentAssessmentValues } from "../types/EstablishmentAssessment"
import { today, formatNumber } from "../../irrigation/formValues"
import { numberDraft, requiredNumber, optionalText } from "../../../services/values"
export function EstablishmentAssessmentForm({ garden, original, saved, cancel }: { garden: string; original?: EstablishmentAssessment; saved: (record: EstablishmentAssessment) => void; cancel: () => void }) {
const editor = useEditor<EstablishmentAssessment, EstablishmentAssessmentValues>(original, (values, key) => establishmentAssessmentApi.create(garden, values, key), (values, version, reason) => { if (!original) throw new Error("Registro indisponível"); return establishmentAssessmentApi.correct(garden, original.id, values, version, reason) }, () => { if (!original) throw new Error("Registro indisponível"); return establishmentAssessmentApi.get(garden, original.id) }, saved, true)
const [draft, setDraft] = useState<{ plantingId: string; assessedOn: string; survivingQuantity: string; method: string; notes: string; responsibleParticipantId: string }>(() => ({
plantingId: original ? original.plantingId : readRoute().search.get('plantingId') || '',
assessedOn: original ? original.assessedOn : today(),
survivingQuantity: original ? numberDraft(original.survivingQuantity) : '',
method: original ? original.method : '',
notes: original ? original.notes ?? "" : '',
responsibleParticipantId: original ? original.responsibleParticipantId ?? "" : ''
}))
const options = useLookups(garden, ['participant', 'planting'])
function change<K extends keyof typeof draft>(key: K, value: typeof draft[K]) { setDraft(current => ({ ...current, [key]: value })) }
function values(): EstablishmentAssessmentValues { if (options.loading || options.error) throw new Error("Aguarde ou recarregue os cadastros de apoio."); return {
plantingId: draft.plantingId,
assessedOn: draft.assessedOn,
survivingQuantity: requiredNumber(draft.survivingQuantity, 'survivingQuantity', 0, false),
method: draft.method.trim(),
notes: optionalText(draft.notes),
responsibleParticipantId: optionalText(draft.responsibleParticipantId),
} }
return <EditorFrame dependenciesReady={!options.loading && !options.error} title={original ? "Corrigir avaliação de pegamento" : "Nova avaliação de pegamento"} busy={editor.busy} uncertain={!!editor.attempt || editor.catalogUncertain} catalogUncertain={editor.catalogUncertain} error={editor.error} reason={original ? editor.reason : undefined} changeReason={original ? editor.setReason : undefined} submit={() => void editor.submit(values)} cancel={cancel} conflict={original ? () => void editor.reloadConflict() : undefined} review={editor.review && <Facts items={[{ label: "Versão", value: editor.review.version }, { label: "Valores atuais", value: <EstablishmentAssessmentReview record={editor.review} /> }]} />} acceptReview={editor.acceptReview}>
{options.loading && <p role="status">Carregando cadastros de apoio…</p>}{options.error && <Failure error={options.error} retry={options.retry} />}
<div className="grid min-w-0 gap-5">
<Group title="Avaliação">
<SelectField name="plantingId" label="Plantio avaliado" required value={draft.plantingId} change={value => change("plantingId", value)} options={options.data?.planting ?? []} error={editor.error} />
<Field name="assessedOn" label="Data da avaliação" required type="date" value={draft.assessedOn} change={value => change("assessedOn", value)} error={editor.error} />
<SelectField name="responsibleParticipantId" label="Responsável pelo trabalho" value={draft.responsibleParticipantId} change={value => change("responsibleParticipantId", value)} options={options.data?.participant ?? []} error={editor.error} />
</Group>
<Group title="Resultado">
<Field name="survivingQuantity" label="Quantidade sobrevivente" required type="integer" value={draft.survivingQuantity} change={value => change("survivingQuantity", value)} error={editor.error} />
<Field name="method" label="Método" required type="text" value={draft.method} change={value => change("method", value)} error={editor.error} />
<Field name="notes" label="Observações" type="textarea" value={draft.notes} change={value => change("notes", value)} error={editor.error} />
</Group>
</div></EditorFrame>
}
export function EstablishmentAssessmentReview({ record }: { record: EstablishmentAssessment }) { const options = useLookups(record.gardenId, ['participant', 'planting']); return <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[{ label: 'Plantio avaliado', value: record.plantingId ? <a className="underline" href={linkTo(readRoute().garden, 'cultivo/plantios', record.plantingId)}>{options.data?.planting?.find(x => x.id === record.plantingId)?.label ?? record.plantingId}</a> : "Não informado" }, { label: 'Data da avaliação', value: record.assessedOn ? record.assessedOn.split("-").reverse().join("/") : "Não informado" }, { label: 'Quantidade sobrevivente', value: record.survivingQuantity === null ? "Não informado" : formatNumber(record.survivingQuantity) }, { label: 'Método', value: record.method }, { label: 'Observações', value: record.notes }, { label: 'Responsável pelo trabalho', value: record.responsibleParticipantId ? <a className="underline" href={linkTo(readRoute().garden, 'atividades/participantes', record.responsibleParticipantId)}>{options.data?.participant?.find(x => x.id === record.responsibleParticipantId)?.label ?? record.responsibleParticipantId}</a> : "Não informado" }]} /></> }
