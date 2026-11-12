import { readRoute } from '../../../routes/navigation'
import { useState } from "react"
import { EditorFrame } from "../../../components/EditorFrame"
import { Field, Facts, Group } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useEditor } from "../../../hooks/useEditor"
import { useLookups } from "../../../hooks/useLookups"
import { institutionApi } from "../services/InstitutionApi"
import type { Institution, InstitutionValues } from "../types/Institution"
import { optionalText } from "../../../services/values"
export function InstitutionForm({ garden, original, saved, cancel }: { garden: string; original?: Institution; saved: (record: Institution) => void; cancel: () => void }) {
const editor = useEditor<Institution, InstitutionValues>(original, (values) => institutionApi.create(values), (values, version, reason) => { if (!original) throw new Error("Registro indisponível"); return institutionApi.correct(original.id, values, version, reason) }, () => { if (!original) throw new Error("Registro indisponível"); return institutionApi.get(original.id) }, saved, false)
const [draft, setDraft] = useState<{ name: string; acronym: string; type: string }>(() => ({
name: original ? original.name : '',
acronym: original ? original.acronym ?? "" : '',
type: original ? original.type ?? "" : ''
}))
const options = useLookups(garden, [])
function change<K extends keyof typeof draft>(key: K, value: typeof draft[K]) { setDraft(current => ({ ...current, [key]: value })) }
function values(): InstitutionValues { if (options.loading || options.error) throw new Error("Aguarde ou recarregue os cadastros de apoio."); return {
name: draft.name.trim(),
acronym: optionalText(draft.acronym),
type: optionalText(draft.type),
} }
return <EditorFrame dependenciesReady={!options.loading && !options.error} requireActiveGarden={false} title={original ? "Corrigir instituição" : "Nova instituição"} busy={editor.busy} uncertain={!!editor.attempt || editor.catalogUncertain} catalogUncertain={editor.catalogUncertain} error={editor.error} reason={original ? editor.reason : undefined} changeReason={original ? editor.setReason : undefined} submit={() => void editor.submit(values)} cancel={cancel} conflict={original ? () => void editor.reloadConflict() : undefined} review={editor.review && <Facts items={[{ label: "Versão", value: editor.review.version }, { label: "Valores atuais", value: <InstitutionReview record={editor.review} /> }]} />} acceptReview={editor.acceptReview}>
{options.loading && <p role="status">Carregando cadastros de apoio…</p>}{options.error && <Failure error={options.error} retry={options.retry} />}
<div className="grid min-w-0 gap-5">
<Group title="Identificação">
<Field name="name" label="Nome" required type="text" value={draft.name} change={value => change("name", value)} error={editor.error} />
<Field name="acronym" label="Sigla" type="text" value={draft.acronym} change={value => change("acronym", value)} error={editor.error} />
<Field name="type" label="Tipo" type="text" value={draft.type} change={value => change("type", value)} error={editor.error} />
</Group></div></EditorFrame>
}
export function InstitutionReview({ record }: { record: Institution }) { const options = useLookups(readRoute().garden, []); return <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[{ label: 'Nome', value: record.name }, { label: 'Sigla', value: record.acronym }, { label: 'Tipo', value: record.type }]} /></> }
