import { readRoute } from '../../../routes/navigation'
import { linkTo } from '../../../routes/routes'
import { useState } from "react"
import { EditorFrame } from "../../../components/EditorFrame"
import { Field, SelectField, Group, Facts } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useEditor } from "../../../hooks/useEditor"
import { useLookups } from "../../../hooks/useLookups"
import { dryingLotApi } from "../services/DryingLotApi"
import type { DryingLot, DryingLotValues } from "../types/DryingLot"
import { today, formatNumber } from "../../irrigation/formValues"
import { numberDraft, requiredNumber, numeric, optionalText } from "../../../services/values"
export function DryingLotForm({ garden, original, saved, cancel }: { garden: string; original?: DryingLot; saved: (record: DryingLot) => void; cancel: () => void }) {
const editor = useEditor<DryingLot, DryingLotValues>(original, (values, key) => dryingLotApi.create(garden, values, key), (values, version, reason) => { if (!original) throw new Error("Registro indisponível"); return dryingLotApi.correct(garden, original.id, values, version, reason) }, () => { if (!original) throw new Error("Registro indisponível"); return dryingLotApi.get(garden, original.id) }, saved, true)
const [draft, setDraft] = useState<{ method: string; startedOn: string; endedOn: string; totalDryMassKg: string; responsibleParticipantId: string; items: { harvestId: string; allocatedMassKg: string }[] }>(() => ({
method: original ? original.method ?? "" : '',
startedOn: original ? original.startedOn : today(),
endedOn: original ? original.endedOn ?? "" : '',
totalDryMassKg: original ? numberDraft(original.totalDryMassKg) : '',
responsibleParticipantId: original ? original.responsibleParticipantId ?? "" : '',
items: original ? original.items.map(item => ({ ...item, allocatedMassKg: numberDraft(item.allocatedMassKg) })) : readRoute().search.get('harvestId') ? [{ harvestId: readRoute().search.get('harvestId') || '', allocatedMassKg: '' }] : []
}))
const options = useLookups(garden, ['harvest', 'participant'])
function change<K extends keyof typeof draft>(key: K, value: typeof draft[K]) { setDraft(current => ({ ...current, [key]: value })) }
function values(): DryingLotValues { if (options.loading || options.error) throw new Error("Aguarde ou recarregue os cadastros de apoio."); return {
method: optionalText(draft.method),
startedOn: draft.startedOn,
endedOn: optionalText(draft.endedOn),
totalDryMassKg: numeric(draft.totalDryMassKg, 'totalDryMassKg', false, 3, false),
responsibleParticipantId: optionalText(draft.responsibleParticipantId),
items: draft.items.map((item, index) => ({ harvestId: item.harvestId, allocatedMassKg: requiredNumber(item.allocatedMassKg, `items[${index}].allocatedMassKg`) })),
} }
return <EditorFrame dependenciesReady={!options.loading && !options.error} title={original ? "Corrigir lote de secagem" : "Novo lote de secagem"} busy={editor.busy} uncertain={!!editor.attempt || editor.catalogUncertain} catalogUncertain={editor.catalogUncertain} error={editor.error} reason={original ? editor.reason : undefined} changeReason={original ? editor.setReason : undefined} submit={() => void editor.submit(values)} cancel={cancel} conflict={original ? () => void editor.reloadConflict() : undefined} review={editor.review && <Facts items={[{ label: "Versão", value: editor.review.version }, { label: "Valores atuais", value: <DryingLotReview record={editor.review} /> }]} />} acceptReview={editor.acceptReview}>
{options.loading && <p role="status">Carregando cadastros de apoio…</p>}{options.error && <Failure error={options.error} retry={options.retry} />}
<div className="grid min-w-0 gap-5">
<Group title="Secagem">
<Field name="method" label="Método" type="text" value={draft.method} change={value => change("method", value)} error={editor.error} />
<Field name="startedOn" label="Data de início" required type="date" value={draft.startedOn} change={value => change("startedOn", value)} error={editor.error} />
<SelectField name="responsibleParticipantId" label="Responsável pelo trabalho" value={draft.responsibleParticipantId} change={value => change("responsibleParticipantId", value)} options={options.data?.participant ?? []} error={editor.error} />
</Group>
{original && <Group title="Encerramento">
{original && <Field name="endedOn" label="Data de encerramento" type="date" value={draft.endedOn} change={value => change("endedOn", value)} error={editor.error} />}
{original && <Field name="totalDryMassKg" label="Massa seca total (kg)" type="decimal" value={draft.totalDryMassKg} change={value => change("totalDryMassKg", value)} error={editor.error} />}
</Group>}
</div>
<section className="form-repeat"><h3>Origens da secagem</h3><p className="hint">Sem rateio de massa seca por origem. O saldo é confirmado pelo servidor.</p>{draft.items.map((item, index) => <Group key={index} title={"Origens da secagem " + (index + 1)}>
<SelectField name={`items[${index}].harvestId`} label="Colheita de origem" required value={item.harvestId} change={value => change("items", draft.items.map((row, i) => i === index ? { ...row, harvestId: value } : row))} options={options.data?.harvest?.map(item => ({ ...item, disabled: item.usableMassKg === null || item.usableMassKg === undefined, label: item.label + (item.usableMassKg === null ? " · pese a massa aproveitável antes de destinar" : "") })) ?? []} error={editor.error} />
<Field name={`items[${index}].allocatedMassKg`} label="Massa destinada (kg)" required type="decimal" value={item.allocatedMassKg} change={value => change("items", draft.items.map((row, i) => i === index ? { ...row, allocatedMassKg: value } : row))} error={editor.error} />
<button type="button" className="danger-outline" onClick={() => change("items", draft.items.filter((_, i) => i !== index))}>Remover item {index + 1}</button></Group>)}
<button type="button" className="secondary mt-4" onClick={() => change("items", [...draft.items, { harvestId: '', allocatedMassKg: '' }])}>Adicionar colheita de origem</button></section>
<div className="grid min-w-0 gap-4 sm:grid-cols-2">
</div></EditorFrame>
}
export function DryingLotReview({ record }: { record: DryingLot }) { const options = useLookups(record.gardenId, ['harvest', 'participant']); return <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[{ label: 'Método', value: record.method }, { label: 'Data de início', value: record.startedOn ? record.startedOn.split("-").reverse().join("/") : "Não informado" }, { label: 'Data de encerramento', value: record.endedOn ? record.endedOn.split("-").reverse().join("/") : "Não informado" }, { label: 'Massa seca total (kg)', value: record.totalDryMassKg === null ? "Não informado" : formatNumber(record.totalDryMassKg) }, { label: 'Responsável pelo trabalho', value: record.responsibleParticipantId ? <a className="underline" href={linkTo(readRoute().garden, 'atividades/participantes', record.responsibleParticipantId)}>{options.data?.participant?.find(x => x.id === record.responsibleParticipantId)?.label ?? record.responsibleParticipantId}</a> : "Não informado" }, { label: 'Origens da secagem', value: <ul className="space-y-4">{record.items.map((item, index) => <li className="border-b border-horta-line p-3" key={index}><Facts items={[{ label: 'Colheita de origem', value: options.data?.harvest?.find(x => x.id === item.harvestId)?.label ?? item.harvestId }, { label: 'Massa destinada (kg)', value: item.allocatedMassKg === null ? "Não informado" : formatNumber(item.allocatedMassKg) }]} /></li>)}</ul> }]} /></> }
