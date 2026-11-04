import { readRoute } from "../../../hooks/useRoute"
import { useState } from "react"
import { EditorFrame } from "../../../components/EditorFrame"
import { Field, Facts } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useEditor } from "../../../hooks/useEditor"
import { useLookups } from "../../../hooks/useLookups"
import { agriculturalInputApi } from "../services/AgriculturalInputApi"
import type { AgriculturalInput, AgriculturalInputValues } from "../types/AgriculturalInput"
import { optionalText } from "../../../services/values"
export function AgriculturalInputForm({ garden, original, saved, cancel }: { garden: string; original?: AgriculturalInput; saved: (record: AgriculturalInput) => void; cancel: () => void }) {
const editor = useEditor<AgriculturalInput, AgriculturalInputValues>(original, (values) => agriculturalInputApi.create(values), (values, version, reason) => { if (!original) throw new Error("Registro indisponível"); return agriculturalInputApi.correct(original.id, values, version, reason) }, () => { if (!original) throw new Error("Registro indisponível"); return agriculturalInputApi.get(original.id) }, saved, false)
const [draft, setDraft] = useState<{ name: string; category: string; referenceUnit: string }>(() => ({
name: original ? original.name : '',
category: original ? original.category ?? "" : '',
referenceUnit: original ? original.referenceUnit : ''
}))
const options = useLookups(garden, [])
function change<K extends keyof typeof draft>(key: K, value: typeof draft[K]) { setDraft(current => ({ ...current, [key]: value })) }
function values(): AgriculturalInputValues { if (options.loading || options.error) throw new Error("Aguarde ou recarregue os cadastros de apoio."); return {
name: draft.name.trim(),
category: optionalText(draft.category),
referenceUnit: draft.referenceUnit.trim(),
} }
return <EditorFrame dependenciesReady={!options.loading && !options.error} requireActiveGarden={false} title={original ? "Corrigir: Insumo" : "Novo: Insumo"} busy={editor.busy} uncertain={!!editor.attempt || editor.catalogUncertain} catalogUncertain={editor.catalogUncertain} error={editor.error} reason={original ? editor.reason : undefined} changeReason={original ? editor.setReason : undefined} submit={() => void editor.submit(values)} cancel={cancel} conflict={original ? () => void editor.reloadConflict() : undefined} review={editor.review && <Facts items={[{ label: "Versão", value: editor.review.version }, { label: "Valores atuais", value: <AgriculturalInputReview record={editor.review} /> }]} />} acceptReview={editor.acceptReview}>
{options.loading && <p role="status">Carregando cadastros de apoio…</p>}{options.error && <Failure error={options.error} retry={options.retry} />}
<div className="grid min-w-0 gap-4 sm:grid-cols-2">
<Field name="name" label="Nome" required type="text" value={draft.name} change={value => change("name", value)} error={editor.error} />
<Field name="category" label="Categoria" type="text" value={draft.category} change={value => change("category", value)} error={editor.error} />
<Field name="referenceUnit" label="Unidade de referência" required type="text" value={draft.referenceUnit} change={value => change("referenceUnit", value)} error={editor.error} />
</div></EditorFrame>
}
export function AgriculturalInputReview({ record }: { record: AgriculturalInput }) { const options = useLookups(readRoute().garden, []); return <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[{ label: 'Nome', value: record.name }, { label: 'Categoria', value: record.category }, { label: 'Unidade de referência', value: record.referenceUnit }]} /></> }
