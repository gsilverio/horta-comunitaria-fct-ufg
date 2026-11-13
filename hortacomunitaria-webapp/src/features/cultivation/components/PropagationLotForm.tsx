import type { DetailItem } from "../../../types/records"
import { readRoute } from '../../../routes/navigation'
import { linkTo } from '../../../routes/routes'
import { useState } from "react"
import { EditorFrame } from "../../../components/EditorFrame"
import { Field, SelectField, Facts, Group } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useEditor } from "../../../hooks/useEditor"
import { useLookups } from "../../../hooks/useLookups"
import { propagationLotApi } from "../services/PropagationLotApi"
import type { PropagationLot, PropagationLotValues } from "../types/PropagationLot"
import { optionalText } from "../../../services/values"
import { enumValue } from "../../../types/enums"
import type { MaterialType } from "../../../types/enums"
import { enumLabels } from "../../../services/labels"
export function PropagationLotForm({ garden, original, saved, cancel }: { garden: string; original?: PropagationLot; saved: (record: PropagationLot) => void; cancel: () => void }) {
const editor = useEditor<PropagationLot, PropagationLotValues>(original, (values) => propagationLotApi.create(garden, values), (values, version, reason) => { if (!original) throw new Error("Registro indisponível"); return propagationLotApi.correct(garden, original.id, values, version, reason) }, () => { if (!original) throw new Error("Registro indisponível"); return propagationLotApi.get(garden, original.id) }, saved, false)
const [draft, setDraft] = useState<{ speciesId: string; cultivarId: string; materialType: string; origin: string; receivedOn: string; notes: string }>(() => ({
speciesId: original ? original.speciesId : readRoute().search.get('speciesId') || '',
cultivarId: original ? original.cultivarId ?? "" : '',
materialType: original ? original.materialType : 'Seedling',
origin: original ? original.origin ?? "" : '',
receivedOn: original ? original.receivedOn ?? "" : '',
notes: original ? original.notes ?? "" : ''
}))
const options = useLookups(garden, ['cultivar', 'species'])
function change<K extends keyof typeof draft>(key: K, value: typeof draft[K]) { setDraft(current => ({ ...current, [key]: value, ...(key === 'speciesId' ? { cultivarId: '' } : {}) })) }
function values(): PropagationLotValues { if (options.loading || options.error) throw new Error("Aguarde ou recarregue os cadastros de apoio."); return {
speciesId: draft.speciesId,
cultivarId: optionalText(draft.cultivarId),
materialType: enumValue<MaterialType>(draft.materialType, ['Seedling', 'Seed', 'Cutting', 'Other']),
origin: optionalText(draft.origin),
receivedOn: optionalText(draft.receivedOn),
notes: optionalText(draft.notes),
} }
return <EditorFrame dependenciesReady={!options.loading && !options.error} title={original ? "Corrigir lote de origem" : "Novo lote de origem"} busy={editor.busy} uncertain={!!editor.attempt || editor.catalogUncertain} catalogUncertain={editor.catalogUncertain} error={editor.error} reason={original ? editor.reason : undefined} changeReason={original ? editor.setReason : undefined} submit={() => void editor.submit(values)} cancel={cancel} conflict={original ? () => void editor.reloadConflict() : undefined} review={editor.review && <Facts items={[{ label: "Versão", value: editor.review.version }, { label: "Valores atuais", value: <PropagationLotReview record={editor.review} /> }]} />} acceptReview={editor.acceptReview}>
{options.loading && <p role="status">Carregando cadastros de apoio…</p>}{options.error && <Failure error={options.error} retry={options.retry} />}
<div className="form-stack">
<Group title="Material">
<SelectField name="speciesId" label="Espécie" required value={draft.speciesId} change={value => change("speciesId", value)} options={options.data?.species ?? []} error={editor.error} />
<SelectField name="cultivarId" label="Cultivar" value={draft.cultivarId} change={value => change("cultivarId", value)} options={options.data?.cultivar?.filter(x => x.speciesId === draft.speciesId) ?? []} error={editor.error} />
<SelectField name="materialType" label="Tipo de material" required value={draft.materialType} change={value => change("materialType", value)} options={[{ id: 'Seedling', label: enumLabels['Seedling'] }, { id: 'Seed', label: enumLabels['Seed'] }, { id: 'Cutting', label: enumLabels['Cutting'] }, { id: 'Other', label: enumLabels['Other'] }]} error={editor.error} />
</Group>
<Group title="Procedência">
<Field name="origin" label="Origem" type="text" value={draft.origin} change={value => change("origin", value)} error={editor.error} />
<Field name="receivedOn" label="Recebido em" type="date" value={draft.receivedOn} change={value => change("receivedOn", value)} error={editor.error} />
<Field name="notes" label="Observações" type="textarea" value={draft.notes} change={value => change("notes", value)} error={editor.error} />
</Group>
</div></EditorFrame>
}
export function PropagationLotReview({ record, extra = [] }: { record: PropagationLot; extra?: DetailItem[] }) { const options = useLookups(record.gardenId, ['cultivar', 'species']); return <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[{ label: 'Espécie', value: record.speciesId ? <a className="underline" href={linkTo(readRoute().garden, 'catalogo/especies', record.speciesId)}>{options.data?.species?.find(x => x.id === record.speciesId)?.label ?? record.speciesId}</a> : "Não informado" }, { label: 'Cultivar', value: record.cultivarId ? <a className="underline" href={linkTo(readRoute().garden, 'catalogo/cultivares', record.cultivarId, new URLSearchParams({ speciesId: record.speciesId }))}>{options.data?.cultivar?.find(x => x.id === record.cultivarId)?.label ?? record.cultivarId}</a> : "Não informado" }, { label: 'Tipo de material', value: enumLabels[record.materialType] }, { label: 'Origem', value: record.origin }, { label: 'Recebido em', value: record.receivedOn ? record.receivedOn.split("-").reverse().join("/") : "Não informado" }, { label: 'Observações', value: record.notes }, ...extra]} /></> }
