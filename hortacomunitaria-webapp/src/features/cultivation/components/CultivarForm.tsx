import { readRoute } from '../../../routes/navigation'
import { useState } from "react"
import { EditorFrame } from "../../../components/EditorFrame"
import { Field, Facts, Group } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useEditor } from "../../../hooks/useEditor"
import { useLookups } from "../../../hooks/useLookups"
import { cultivarApi } from "../services/CultivarApi"
import type { Cultivar, CultivarValues } from "../types/Cultivar"
import { optionalText } from "../../../services/values"
export function CultivarForm({ garden, parent, original, saved, cancel }: { garden: string; parent: string; original?: Cultivar; saved: (record: Cultivar) => void; cancel: () => void }) {
const editor = useEditor<Cultivar, CultivarValues>(original, (values) => cultivarApi.create(parent, values), (values, version, reason) => { if (!original) throw new Error("Registro indisponível"); return cultivarApi.correct(parent, original.id, values, version, reason) }, () => { if (!original) throw new Error("Registro indisponível"); return cultivarApi.get(parent, original.id) }, saved, false)
const [draft, setDraft] = useState<{ name: string; description: string }>(() => ({
name: original ? original.name : '',
description: original ? original.description ?? "" : ''
}))
const options = useLookups(garden, [])
function change<K extends keyof typeof draft>(key: K, value: typeof draft[K]) { setDraft(current => ({ ...current, [key]: value })) }
function values(): CultivarValues { if (options.loading || options.error) throw new Error("Aguarde ou recarregue os cadastros de apoio."); return {
name: draft.name.trim(),
description: optionalText(draft.description),
} }
return <EditorFrame dependenciesReady={!options.loading && !options.error} requireActiveGarden={false} title={original ? "Corrigir cultivar" : "Novo cultivar"} busy={editor.busy} uncertain={!!editor.attempt || editor.catalogUncertain} catalogUncertain={editor.catalogUncertain} error={editor.error} reason={original ? editor.reason : undefined} changeReason={original ? editor.setReason : undefined} submit={() => void editor.submit(values)} cancel={cancel} conflict={original ? () => void editor.reloadConflict() : undefined} review={editor.review && <Facts items={[{ label: "Versão", value: editor.review.version }, { label: "Valores atuais", value: <CultivarReview record={editor.review} /> }]} />} acceptReview={editor.acceptReview}>
{options.loading && <p role="status">Carregando cadastros de apoio…</p>}{options.error && <Failure error={options.error} retry={options.retry} />}
<div className="form-stack">
<Group title="Identificação">
<Field name="name" label="Nome" required type="text" value={draft.name} change={value => change("name", value)} error={editor.error} />
</Group>
<Group title="Descrição" hint="Opcional.">
<Field name="description" label="Descrição" type="textarea" value={draft.description} change={value => change("description", value)} error={editor.error} />
</Group></div></EditorFrame>
}
export function CultivarReview({ record }: { record: Cultivar }) { const options = useLookups(readRoute().garden, []); return <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[{ label: 'Nome', value: record.name }, { label: 'Descrição', value: record.description }]} /></> }
