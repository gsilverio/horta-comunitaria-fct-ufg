import { routeHref } from "../../../hooks/useRoute"
import { readRoute } from "../../../hooks/useRoute"
import { useState } from "react"
import { EditorFrame } from "../../../components/EditorFrame"
import { Field, SelectField, Facts } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useEditor } from "../../../hooks/useEditor"
import { useLookups } from "../../../hooks/useLookups"
import { harvestApi } from "../services/HarvestApi"
import type { Harvest, HarvestValues } from "../types/Harvest"
import { today, formatNumber } from "../../irrigation/formValues"
import { numberDraft, requiredNumber, numeric, optionalText } from "../../../services/values"
export function HarvestForm({ garden, original, saved, cancel }: { garden: string; original?: Harvest; saved: (record: Harvest) => void; cancel: () => void }) {
const editor = useEditor<Harvest, HarvestValues>(original, (values, key) => harvestApi.create(garden, values, key), (values, version, reason) => { if (!original) throw new Error("Registro indisponível"); return harvestApi.correct(garden, original.id, values, version, reason) }, () => { if (!original) throw new Error("Registro indisponível"); return harvestApi.get(garden, original.id) }, saved, true)
const [draft, setDraft] = useState<{ cycleId: string; harvestedOn: string; freshMassKg: string; usableMassKg: string; lossReason: string; responsibleParticipantId: string }>(() => ({
cycleId: original ? original.cycleId : readRoute().search.get('cycleId') || '',
harvestedOn: original ? original.harvestedOn : today(),
freshMassKg: original ? numberDraft(original.freshMassKg) : '',
usableMassKg: original ? numberDraft(original.usableMassKg) : '',
lossReason: original ? original.lossReason ?? "" : '',
responsibleParticipantId: original ? original.responsibleParticipantId ?? "" : ''
}))
const options = useLookups(garden, ['cultivationCycle', 'participant'])
function change<K extends keyof typeof draft>(key: K, value: typeof draft[K]) { setDraft(current => ({ ...current, [key]: value })) }
function values(): HarvestValues { if (options.loading || options.error) throw new Error("Aguarde ou recarregue os cadastros de apoio."); return {
cycleId: draft.cycleId,
harvestedOn: draft.harvestedOn,
freshMassKg: requiredNumber(draft.freshMassKg, 'freshMassKg', 3, false),
usableMassKg: numeric(draft.usableMassKg, 'usableMassKg', false, 3, false),
lossReason: optionalText(draft.lossReason),
responsibleParticipantId: optionalText(draft.responsibleParticipantId),
} }
return <EditorFrame dependenciesReady={!options.loading && !options.error} title={original ? "Corrigir: Colheita" : "Novo: Colheita"} busy={editor.busy} uncertain={!!editor.attempt || editor.catalogUncertain} catalogUncertain={editor.catalogUncertain} error={editor.error} reason={original ? editor.reason : undefined} changeReason={original ? editor.setReason : undefined} submit={() => void editor.submit(values)} cancel={cancel} conflict={original ? () => void editor.reloadConflict() : undefined} review={editor.review && <Facts items={[{ label: "Versão", value: editor.review.version }, { label: "Valores atuais", value: <HarvestReview record={editor.review} /> }]} />} acceptReview={editor.acceptReview}>
{options.loading && <p role="status">Carregando cadastros de apoio…</p>}{options.error && <Failure error={options.error} retry={options.retry} />}
<div className="grid min-w-0 gap-4 sm:grid-cols-2">
<SelectField name="cycleId" label="Ciclo" required value={draft.cycleId} change={value => change("cycleId", value)} options={options.data?.cultivationCycle ?? []} error={editor.error} />
<Field name="harvestedOn" label="Data da colheita" required type="date" value={draft.harvestedOn} change={value => change("harvestedOn", value)} error={editor.error} />
<Field name="freshMassKg" label="Massa fresca (kg)" required type="decimal" value={draft.freshMassKg} change={value => change("freshMassKg", value)} error={editor.error} />
<Field name="usableMassKg" label="Massa aproveitável (kg)" type="decimal" value={draft.usableMassKg} change={value => change("usableMassKg", value)} error={editor.error} />
<Field name="lossReason" label="Motivo da perda" type="text" value={draft.lossReason} change={value => change("lossReason", value)} error={editor.error} />
<SelectField name="responsibleParticipantId" label="Responsável pelo trabalho" value={draft.responsibleParticipantId} change={value => change("responsibleParticipantId", value)} options={options.data?.participant ?? []} error={editor.error} />
</div></EditorFrame>
}
export function HarvestReview({ record }: { record: Harvest }) { const options = useLookups(record.gardenId, ['cultivationCycle', 'participant']); return <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[{ label: 'Ciclo', value: record.cycleId ? <a className="underline" href={routeHref(readRoute().garden, 'cultivo', 'ciclos', record.cycleId)}>{options.data?.cultivationCycle?.find(x => x.id === record.cycleId)?.label ?? record.cycleId}</a> : "Não informado" }, { label: 'Data da colheita', value: record.harvestedOn ? record.harvestedOn.split("-").reverse().join("/") : "Não informado" }, { label: 'Massa fresca (kg)', value: record.freshMassKg === null ? "Não informado" : formatNumber(record.freshMassKg) }, { label: 'Massa aproveitável (kg)', value: record.usableMassKg === null ? "Não informado" : formatNumber(record.usableMassKg) }, { label: 'Motivo da perda', value: record.lossReason }, { label: 'Responsável pelo trabalho', value: record.responsibleParticipantId ? <a className="underline" href={routeHref(readRoute().garden, 'atividades', 'participantes', record.responsibleParticipantId)}>{options.data?.participant?.find(x => x.id === record.responsibleParticipantId)?.label ?? record.responsibleParticipantId}</a> : "Não informado" }]} /></> }
