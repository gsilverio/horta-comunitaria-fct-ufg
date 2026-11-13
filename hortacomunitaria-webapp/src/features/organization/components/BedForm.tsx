import { useState } from "react"
import { EditorFrame } from "../../../components/EditorFrame"
import { Field, Facts, Group } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useEditor } from "../../../hooks/useEditor"
import { useLookups } from "../../../hooks/useLookups"
import { bedApi } from "../services/BedApi"
import type { Bed, BedValues } from "../types/Bed"
import { optionalText } from "../../../services/values"
export function BedForm({ garden, original, saved, cancel }: { garden: string; original?: Bed; saved: (record: Bed) => void; cancel: () => void }) {
const editor = useEditor<Bed, BedValues>(original, (values) => bedApi.create(garden, values), (values, version, reason) => { if (!original) throw new Error("Registro indisponível"); return bedApi.correct(garden, original.id, values, version, reason) }, () => { if (!original) throw new Error("Registro indisponível"); return bedApi.get(garden, original.id) }, saved, false)
const [draft, setDraft] = useState<{ name: string; location: string }>(() => ({
name: original ? original.name : '',
location: original ? original.location ?? "" : ''
}))
const options = useLookups(garden, [])
function change<K extends keyof typeof draft>(key: K, value: typeof draft[K]) { setDraft(current => ({ ...current, [key]: value })) }
function values(): BedValues { if (options.loading || options.error) throw new Error("Aguarde ou recarregue os cadastros de apoio."); return {
name: draft.name.trim(),
location: optionalText(draft.location),
} }
return <EditorFrame dependenciesReady={!options.loading && !options.error} title={original ? "Editar canteiro" : "Novo canteiro"} submitLabel={original ? "Salvar alterações" : "Continuar"} description="Dê um nome ao canteiro. Na próxima etapa, você confirma a área e o setor." busy={editor.busy} uncertain={!!editor.attempt || editor.catalogUncertain} catalogUncertain={editor.catalogUncertain} error={editor.error} reason={original ? editor.reason : undefined} changeReason={original ? editor.setReason : undefined} submit={() => void editor.submit(values)} cancel={cancel} conflict={original ? () => void editor.reloadConflict() : undefined} review={editor.review && <Facts items={[{ label: "Versão", value: editor.review.version }, { label: "Valores atuais", value: <BedReview record={editor.review} /> }]} />} acceptReview={editor.acceptReview}>
{options.loading && <p role="status">Carregando cadastros de apoio…</p>}{options.error && <Failure error={options.error} retry={options.retry} />}
<div className="form-stack">
<Group title="Identificação" hint="A área e o setor são confirmados na etapa seguinte.">
<Field name="name" label="Nome do canteiro" required type="text" value={draft.name} change={value => change("name", value)} error={editor.error} />
<Field name="location" label="Localização" type="text" value={draft.location} change={value => change("location", value)} error={editor.error} />
</Group></div></EditorFrame>
}
export function BedReview({ record }: { record: Bed }) { const options = useLookups(record.gardenId, []); return <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[{ label: 'Nome', value: record.name }, { label: 'Localização', value: record.location }]} /></> }
