import { useState } from "react"
import { EditorFrame } from "../../../components/EditorFrame"
import { Field, Facts, Group } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useEditor } from "../../../hooks/useEditor"
import { useLookups } from "../../../hooks/useLookups"
import { irrigationSectorApi } from "../services/IrrigationSectorApi"
import type { IrrigationSector, IrrigationSectorValues } from "../types/IrrigationSector"
import { optionalText } from "../../../services/values"
export function IrrigationSectorForm({ garden, original, saved, cancel }: { garden: string; original?: IrrigationSector; saved: (record: IrrigationSector) => void; cancel: () => void }) {
const editor = useEditor<IrrigationSector, IrrigationSectorValues>(original, (values) => irrigationSectorApi.create(garden, values), (values, version, reason) => { if (!original) throw new Error("Registro indisponível"); return irrigationSectorApi.correct(garden, original.id, values, version, reason) }, () => { if (!original) throw new Error("Registro indisponível"); return irrigationSectorApi.get(garden, original.id) }, saved, false)
const [draft, setDraft] = useState<{ name: string; description: string }>(() => ({
name: original ? original.name : '',
description: original ? original.description ?? "" : ''
}))
const options = useLookups(garden, [])
function change<K extends keyof typeof draft>(key: K, value: typeof draft[K]) { setDraft(current => ({ ...current, [key]: value })) }
function values(): IrrigationSectorValues { if (options.loading || options.error) throw new Error("Aguarde ou recarregue os cadastros de apoio."); return {
name: draft.name.trim(),
description: optionalText(draft.description),
} }
return <EditorFrame dependenciesReady={!options.loading && !options.error} title={original ? "Editar setor" : "Novo setor"} submitLabel={original ? "Salvar alterações" : "Cadastrar setor"} description="Dê um nome que ajude a reconhecer esta área da horta." busy={editor.busy} uncertain={!!editor.attempt || editor.catalogUncertain} catalogUncertain={editor.catalogUncertain} error={editor.error} reason={original ? editor.reason : undefined} changeReason={original ? editor.setReason : undefined} submit={() => void editor.submit(values)} cancel={cancel} conflict={original ? () => void editor.reloadConflict() : undefined} review={editor.review && <Facts items={[{ label: "Versão", value: editor.review.version }, { label: "Valores atuais", value: <IrrigationSectorReview record={editor.review} /> }]} />} acceptReview={editor.acceptReview}>
{options.loading && <p role="status">Carregando cadastros de apoio…</p>}{options.error && <Failure error={options.error} retry={options.retry} />}
<div className="grid min-w-0 gap-5">
<Group title="Identificação">
<Field name="name" label="Nome do setor" required type="text" value={draft.name} change={value => change("name", value)} error={editor.error} />
<Field name="description" label="Descrição" type="textarea" value={draft.description} change={value => change("description", value)} error={editor.error} />
</Group></div></EditorFrame>
}
export function IrrigationSectorReview({ record }: { record: IrrigationSector }) { const options = useLookups(record.gardenId, []); return <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[{ label: 'Nome', value: record.name }, { label: 'Descrição', value: record.description }]} /></> }
