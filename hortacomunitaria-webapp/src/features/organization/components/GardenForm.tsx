import { readRoute } from "../../../hooks/useRoute"
import { useState } from "react"
import { EditorFrame } from "../../../components/EditorFrame"
import { Field, Facts } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useEditor } from "../../../hooks/useEditor"
import { useLookups } from "../../../hooks/useLookups"
import { gardenApi } from "../services/GardenApi"
import type { Garden, GardenValues } from "../types/Garden"
import { formatNumber } from "../../irrigation/formValues"
import { numberDraft, numeric, optionalText } from "../../../services/values"
export function GardenForm({ garden, original, saved, cancel }: { garden: string; original?: Garden; saved: (record: Garden) => void; cancel: () => void }) {
const editor = useEditor<Garden, GardenValues>(original, (values) => gardenApi.create(values), (values, version, reason) => { if (!original) throw new Error("Registro indisponível"); return gardenApi.correct(original.id, values, version, reason) }, () => { if (!original) throw new Error("Registro indisponível"); return gardenApi.get(original.id) }, saved, false)
const [draft, setDraft] = useState<{ name: string; location: string; institutionalAreaM2: string; operationalAreaM2: string; experimentalAreaM2: string }>(() => ({
name: original ? original.name : '',
location: original ? original.location ?? "" : '',
institutionalAreaM2: original ? numberDraft(original.institutionalAreaM2) : '',
operationalAreaM2: original ? numberDraft(original.operationalAreaM2) : '',
experimentalAreaM2: original ? numberDraft(original.experimentalAreaM2) : ''
}))
const options = useLookups(garden, [])
function change<K extends keyof typeof draft>(key: K, value: typeof draft[K]) { setDraft(current => ({ ...current, [key]: value })) }
function values(): GardenValues { if (options.loading || options.error) throw new Error("Aguarde ou recarregue os cadastros de apoio."); return {
name: draft.name.trim(),
location: optionalText(draft.location),
institutionalAreaM2: numeric(draft.institutionalAreaM2, 'institutionalAreaM2', false, 3, false),
operationalAreaM2: numeric(draft.operationalAreaM2, 'operationalAreaM2', false, 3, false),
experimentalAreaM2: numeric(draft.experimentalAreaM2, 'experimentalAreaM2', false, 3, false),
} }
return <EditorFrame dependenciesReady={!options.loading && !options.error} requireActiveGarden={false} title={original ? "Editar horta" : "Nova horta"} submitLabel={original ? "Salvar alterações" : "Cadastrar horta"} description="Comece pelo nome da horta. Os demais dados podem ser preenchidos depois." busy={editor.busy} uncertain={!!editor.attempt || editor.catalogUncertain} catalogUncertain={editor.catalogUncertain} error={editor.error} reason={original ? editor.reason : undefined} changeReason={original ? editor.setReason : undefined} submit={() => void editor.submit(values)} cancel={cancel} conflict={original ? () => void editor.reloadConflict() : undefined} review={editor.review && <Facts items={[{ label: "Versão", value: editor.review.version }, { label: "Valores atuais", value: <GardenReview record={editor.review} /> }]} />} acceptReview={editor.acceptReview}>
{options.loading && <p role="status">Carregando cadastros de apoio…</p>}{options.error && <Failure error={options.error} retry={options.retry} />}
<div className="grid min-w-0 gap-5">
<Field name="name" label="Nome da horta" required type="text" value={draft.name} change={value => change("name", value)} error={editor.error} />
<Field name="location" label="Localização" type="text" value={draft.location} change={value => change("location", value)} error={editor.error} />
<details className="organization-disclosure"><summary>Informar áreas da horta (opcional)</summary><p className="hint">Preencha somente as áreas conhecidas. Você pode completar depois.</p><div className="grid gap-5"><Field name="institutionalAreaM2" label="Área institucional (m²)" type="decimal" value={draft.institutionalAreaM2} change={value => change("institutionalAreaM2", value)} error={editor.error} />
<Field name="operationalAreaM2" label="Área operacional (m²)" type="decimal" value={draft.operationalAreaM2} change={value => change("operationalAreaM2", value)} error={editor.error} />
<Field name="experimentalAreaM2" label="Área experimental (m²)" type="decimal" value={draft.experimentalAreaM2} change={value => change("experimentalAreaM2", value)} error={editor.error} />
</div></details></div></EditorFrame>
}
export function GardenReview({ record }: { record: Garden }) { const options = useLookups(readRoute().garden, []); return <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[{ label: 'Nome', value: record.name }, { label: 'Localização', value: record.location }, { label: 'Área institucional (m²)', value: record.institutionalAreaM2 === null ? "Não informado" : formatNumber(record.institutionalAreaM2) }, { label: 'Área operacional (m²)', value: record.operationalAreaM2 === null ? "Não informado" : formatNumber(record.operationalAreaM2) }, { label: 'Área experimental (m²)', value: record.experimentalAreaM2 === null ? "Não informado" : formatNumber(record.experimentalAreaM2) }]} /></> }
