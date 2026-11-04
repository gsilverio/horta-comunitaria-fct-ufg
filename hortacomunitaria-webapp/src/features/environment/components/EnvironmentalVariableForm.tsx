import { readRoute } from "../../../hooks/useRoute"
import { useState } from "react"
import { EditorFrame } from "../../../components/EditorFrame"
import { Field, Facts, Toggle } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useEditor } from "../../../hooks/useEditor"
import { useLookups } from "../../../hooks/useLookups"
import { environmentalVariableApi } from "../services/EnvironmentalVariableApi"
import type { EnvironmentalVariable, EnvironmentalVariableValues } from "../types/EnvironmentalVariable"
import { optionalText } from "../../../services/values"
export function EnvironmentalVariableForm({ garden, original, saved, cancel }: { garden: string; original?: EnvironmentalVariable; saved: (record: EnvironmentalVariable) => void; cancel: () => void }) {
const editor = useEditor<EnvironmentalVariable, EnvironmentalVariableValues>(original, (values) => environmentalVariableApi.create(values), (values, version, reason) => { if (!original) throw new Error("Registro indisponível"); return environmentalVariableApi.correct(original.id, values, version, reason) }, () => { if (!original) throw new Error("Registro indisponível"); return environmentalVariableApi.get(original.id) }, saved, false)
const [draft, setDraft] = useState<{ isSoilMoisture: boolean; name: string; quantity: string; referenceUnit: string; description: string }>(() => ({
isSoilMoisture: original?.isSoilMoisture ?? false,
name: original ? original.name : '',
quantity: original ? original.quantity : '',
referenceUnit: original ? original.referenceUnit : '',
description: original ? original.description ?? "" : ''
}))
const options = useLookups(garden, [])
function change<K extends keyof typeof draft>(key: K, value: typeof draft[K]) { setDraft(current => ({ ...current, [key]: value })) }
function values(): EnvironmentalVariableValues { if (options.loading || options.error) throw new Error("Aguarde ou recarregue os cadastros de apoio."); return {
isSoilMoisture: draft.isSoilMoisture,
name: draft.name.trim(),
quantity: draft.quantity.trim(),
referenceUnit: draft.referenceUnit.trim(),
description: optionalText(draft.description),
} }
return <EditorFrame dependenciesReady={!options.loading && !options.error} requireActiveGarden={false} title={original ? "Corrigir: Variável ambiental" : "Novo: Variável ambiental"} busy={editor.busy} uncertain={!!editor.attempt || editor.catalogUncertain} catalogUncertain={editor.catalogUncertain} error={editor.error} reason={original ? editor.reason : undefined} changeReason={original ? editor.setReason : undefined} submit={() => void editor.submit(values)} cancel={cancel} conflict={original ? () => void editor.reloadConflict() : undefined} review={editor.review && <Facts items={[{ label: "Versão", value: editor.review.version }, { label: "Valores atuais", value: <EnvironmentalVariableReview record={editor.review} /> }]} />} acceptReview={editor.acceptReview}>
{options.loading && <p role="status">Carregando cadastros de apoio…</p>}{options.error && <Failure error={options.error} retry={options.retry} />}
<div className="grid min-w-0 gap-4 sm:grid-cols-2">
<Toggle label="Esta variável mede umidade do solo" checked={draft.isSoilMoisture} change={value => change("isSoilMoisture", value)} />
<Field name="name" label="Nome" required type="text" value={draft.name} change={value => change("name", value)} error={editor.error} />
<Field name="quantity" label="Grandeza" required type="text" value={draft.quantity} change={value => change("quantity", value)} error={editor.error} />
<Field name="referenceUnit" label="Unidade de referência" required type="text" value={draft.referenceUnit} change={value => change("referenceUnit", value)} error={editor.error} />
<Field name="description" label="Descrição" type="textarea" value={draft.description} change={value => change("description", value)} error={editor.error} />
</div></EditorFrame>
}
export function EnvironmentalVariableReview({ record }: { record: EnvironmentalVariable }) { const options = useLookups(readRoute().garden, []); return <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[{ label: 'Nome', value: record.name }, { label: 'Umidade do solo', value: record.isSoilMoisture ? 'Sim' : 'Não' }, { label: 'Grandeza', value: record.quantity }, { label: 'Unidade de referência', value: record.referenceUnit }, { label: 'Descrição', value: record.description }]} /></> }
