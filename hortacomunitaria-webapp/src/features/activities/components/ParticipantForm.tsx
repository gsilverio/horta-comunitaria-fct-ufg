import type { DetailItem } from "../../../types/records"
import { useState } from "react"
import { EditorFrame } from "../../../components/EditorFrame"
import { Field, Facts, Group } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useEditor } from "../../../hooks/useEditor"
import { useLookups } from "../../../hooks/useLookups"
import { participantApi } from "../services/ParticipantApi"
import type { Participant, ParticipantValues } from "../types/Participant"
import { optionalText } from "../../../services/values"
export function ParticipantForm({ garden, original, saved, cancel }: { garden: string; original?: Participant; saved: (record: Participant) => void; cancel: () => void }) {
const editor = useEditor<Participant, ParticipantValues>(original, (values) => participantApi.create(garden, values), (values, version, reason) => { if (!original) throw new Error("Registro indisponível"); return participantApi.correct(garden, original.id, values, version, reason) }, () => { if (!original) throw new Error("Registro indisponível"); return participantApi.get(garden, original.id) }, saved, false)
const [draft, setDraft] = useState<{ displayName: string; category: string }>(() => ({
displayName: original ? original.displayName : '',
category: original ? original.category ?? "" : ''
}))
const options = useLookups(garden, [])
function change<K extends keyof typeof draft>(key: K, value: typeof draft[K]) { setDraft(current => ({ ...current, [key]: value })) }
function values(): ParticipantValues { if (options.loading || options.error) throw new Error("Aguarde ou recarregue os cadastros de apoio."); return {
displayName: draft.displayName.trim(),
category: optionalText(draft.category),
} }
return <EditorFrame dependenciesReady={!options.loading && !options.error} title={original ? "Corrigir participante" : "Novo participante"} busy={editor.busy} uncertain={!!editor.attempt || editor.catalogUncertain} catalogUncertain={editor.catalogUncertain} error={editor.error} reason={original ? editor.reason : undefined} changeReason={original ? editor.setReason : undefined} submit={() => void editor.submit(values)} cancel={cancel} conflict={original ? () => void editor.reloadConflict() : undefined} review={editor.review && <Facts items={[{ label: "Versão", value: editor.review.version }, { label: "Valores atuais", value: <ParticipantReview record={editor.review} /> }]} />} acceptReview={editor.acceptReview}>
{options.loading && <p role="status">Carregando cadastros de apoio…</p>}{options.error && <Failure error={options.error} retry={options.retry} />}
<div className="form-stack">
<Group title="Identificação">
<Field name="displayName" label="Nome de exibição" required type="text" value={draft.displayName} change={value => change("displayName", value)} error={editor.error} />
<Field name="category" label="Categoria" type="text" value={draft.category} change={value => change("category", value)} error={editor.error} />
</Group></div></EditorFrame>
}
export function ParticipantReview({ record, extra = [] }: { record: Participant; extra?: DetailItem[] }) { const options = useLookups(record.gardenId, []); return <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[{ label: 'Nome de exibição', value: record.displayName }, { label: 'Categoria', value: record.category }, ...extra]} /></> }
