import { readRoute } from '../../../routes/navigation'
import { useState } from "react"
import { EditorFrame } from "../../../components/EditorFrame"
import { Field, Facts, Group } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useEditor } from "../../../hooks/useEditor"
import { useLookups } from "../../../hooks/useLookups"
import { speciesApi } from "../services/SpeciesApi"
import type { Species, SpeciesValues } from "../types/Species"
import { optionalText } from "../../../services/values"
export function SpeciesForm({ garden, original, saved, cancel }: { garden: string; original?: Species; saved: (record: Species) => void; cancel: () => void }) {
const editor = useEditor<Species, SpeciesValues>(original, (values) => speciesApi.create(values), (values, version, reason) => { if (!original) throw new Error("Registro indisponível"); return speciesApi.correct(original.id, values, version, reason) }, () => { if (!original) throw new Error("Registro indisponível"); return speciesApi.get(original.id) }, saved, false)
const [draft, setDraft] = useState<{ commonName: string; scientificName: string; description: string }>(() => ({
commonName: original ? original.commonName : '',
scientificName: original ? original.scientificName ?? "" : '',
description: original ? original.description ?? "" : ''
}))
const options = useLookups(garden, [])
function change<K extends keyof typeof draft>(key: K, value: typeof draft[K]) { setDraft(current => ({ ...current, [key]: value })) }
function values(): SpeciesValues { if (options.loading || options.error) throw new Error("Aguarde ou recarregue os cadastros de apoio."); return {
commonName: draft.commonName.trim(),
scientificName: optionalText(draft.scientificName),
description: optionalText(draft.description),
} }
return <EditorFrame dependenciesReady={!options.loading && !options.error} requireActiveGarden={false} title={original ? "Corrigir espécie" : "Nova espécie"} submitLabel={original ? "Salvar alterações" : "Cadastrar espécie"} description="Informe o nome da planta, por exemplo Alface. Nome científico e descrição são opcionais." busy={editor.busy} uncertain={!!editor.attempt || editor.catalogUncertain} catalogUncertain={editor.catalogUncertain} error={editor.error} reason={original ? editor.reason : undefined} changeReason={original ? editor.setReason : undefined} submit={() => void editor.submit(values)} cancel={cancel} conflict={original ? () => void editor.reloadConflict() : undefined} review={editor.review && <Facts items={[{ label: "Versão", value: editor.review.version }, { label: "Valores atuais", value: <SpeciesReview record={editor.review} /> }]} />} acceptReview={editor.acceptReview}>
{options.loading && <p role="status">Carregando cadastros de apoio…</p>}{options.error && <Failure error={options.error} retry={options.retry} />}
<div className="form-stack">
<Group title="Identificação">
<Field name="commonName" label="Nome comum" required type="text" value={draft.commonName} change={value => change("commonName", value)} error={editor.error} />
<Field name="scientificName" label="Nome científico" type="text" value={draft.scientificName} change={value => change("scientificName", value)} error={editor.error} />
</Group>
<Group title="Descrição" hint="Opcional. Características, usos ou cuidados da espécie.">
<Field name="description" label="Descrição" type="textarea" value={draft.description} change={value => change("description", value)} error={editor.error} />
</Group></div></EditorFrame>
}
export function SpeciesReview({ record }: { record: Species }) { const options = useLookups(readRoute().garden, []); return <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[{ label: 'Nome comum', value: record.commonName }, { label: 'Nome científico', value: record.scientificName }, { label: 'Descrição', value: record.description }]} /></> }
