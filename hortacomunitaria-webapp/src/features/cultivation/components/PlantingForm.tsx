import { readRoute } from '../../../routes/navigation'
import { linkTo } from '../../../routes/routes'
import type { CultivationCycle } from '../types/CultivationCycle'
import { visualDensity } from '../visualDensity'
import { useState } from "react"
import { EditorFrame } from "../../../components/EditorFrame"
import { Field, SelectField, Facts, Group } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useEditor } from "../../../hooks/useEditor"
import { useLookups } from "../../../hooks/useLookups"
import { plantingApi } from "../services/PlantingApi"
import type { Planting, PlantingValues } from "../types/Planting"
import { today, formatNumber } from "../../irrigation/formValues"
import { numberDraft, requiredNumber, numeric, optionalText } from "../../../services/values"
import { enumValue } from "../../../types/enums"
import type { PlantingKind } from "../../../types/enums"
import { enumLabels } from "../../../services/labels"
export function PlantingForm({ garden, original, saved, cancel, fixedCycle, speciesName }: { fixedCycle?: CultivationCycle; speciesName?: string; garden: string; original?: Planting; saved: (record: Planting) => void; cancel: () => void }) {
const editor = useEditor<Planting, PlantingValues>(original, (values, key) => plantingApi.create(garden, values, key), (values, version, reason) => { if (!original) throw new Error("Registro indisponível"); return plantingApi.correct(garden, original.id, values, version, reason) }, () => { if (!original) throw new Error("Registro indisponível"); return plantingApi.get(garden, original.id) }, saved, true)
const [draft, setDraft] = useState<{ cycleId: string; propagationLotId: string; replacedPlantingId: string; plantedOn: string; kind: string; plantedQuantity: string; plantSpacingCm: string; rowSpacingCm: string; responsibleParticipantId: string }>(() => ({
cycleId: original ? original.cycleId : fixedCycle?.id ?? readRoute().search.get('cycleId') ?? '',
propagationLotId: original ? original.propagationLotId ?? "" : '',
replacedPlantingId: original ? original.replacedPlantingId ?? "" : '',
plantedOn: original ? original.plantedOn : fixedCycle?.startedOn ?? today(),
kind: original ? original.kind : 'Initial',
plantedQuantity: original ? numberDraft(original.plantedQuantity) : '',
plantSpacingCm: original ? numberDraft(original.plantSpacingCm) : '',
rowSpacingCm: original ? numberDraft(original.rowSpacingCm) : '',
responsibleParticipantId: original ? original.responsibleParticipantId ?? "" : ''
}))
const options = useLookups(garden, ['cultivationCycle', 'participant', 'planting', 'propagationLot'])
function change<K extends keyof typeof draft>(key: K, value: typeof draft[K]) { setDraft(current => ({ ...current, [key]: value, ...(key === 'cycleId' ? { propagationLotId: '', replacedPlantingId: '' } : {}) })) }
function values(): PlantingValues { if (options.loading || options.error) throw new Error("Aguarde ou recarregue os cadastros de apoio."); return {
cycleId: draft.cycleId,
propagationLotId: optionalText(draft.propagationLotId),
replacedPlantingId: optionalText(draft.replacedPlantingId),
plantedOn: draft.plantedOn,
kind: enumValue<PlantingKind>(draft.kind, ['Initial', 'Replacement']),
plantedQuantity: requiredNumber(draft.plantedQuantity, 'plantedQuantity', 0, false),
plantSpacingCm: numeric(draft.plantSpacingCm, 'plantSpacingCm', false, 3, false),
rowSpacingCm: numeric(draft.rowSpacingCm, 'rowSpacingCm', false, 3, false),
responsibleParticipantId: optionalText(draft.responsibleParticipantId),
} }
return <EditorFrame dependenciesReady={!options.loading && !options.error} title={original ? "Corrigir plantio" : fixedCycle ? `Registrar plantio de ${speciesName ?? "cultivo"}` : "Novo plantio"} submitLabel={fixedCycle ? "Registrar plantio" : "Salvar registro"} cancelLabel={fixedCycle ? "Voltar ao canteiro" : "Cancelar"} description={fixedCycle ? "O cultivo já está cadastrado. Informe a data e a quantidade quando o plantio for realizado, ou volte ao canteiro para registrar depois." : undefined} busy={editor.busy} uncertain={!!editor.attempt || editor.catalogUncertain} catalogUncertain={editor.catalogUncertain} error={editor.error} reason={original ? editor.reason : undefined} changeReason={original ? editor.setReason : undefined} submit={() => void editor.submit(values)} cancel={cancel} conflict={original ? () => void editor.reloadConflict() : undefined} review={editor.review && <Facts items={[{ label: "Versão", value: editor.review.version }, { label: "Valores atuais", value: <PlantingReview record={editor.review} /> }]} />} acceptReview={editor.acceptReview}>
{options.loading && <p role="status">Carregando cadastros de apoio…</p>}{options.error && <Failure error={options.error} retry={options.retry} />}
{fixedCycle ? <div className="grid gap-5">
<Field name="plantedOn" label="Data do plantio" required type="date" value={draft.plantedOn} change={value => change('plantedOn', value)} error={editor.error} />
<Field name="plantedQuantity" label="Quantidade plantada" required type="integer" value={draft.plantedQuantity} change={value => change('plantedQuantity', value)} error={editor.error} hint="Quantidade de plantas ou mudas efetivamente plantadas." />
{draft.kind === 'Initial' && /^\d+$/.test(draft.plantedQuantity) && Number.isSafeInteger(Number(draft.plantedQuantity)) && Number(draft.plantedQuantity) > 0 && <p className="hint" aria-live="polite">Densidade visual deste plantio: {visualDensity(Number(draft.plantedQuantity), fixedCycle.occupiedAreaM2)?.toLocaleLowerCase('pt-BR')}. O panorama usa símbolos para representar a quantidade; outros plantios iniciais do cultivo também entram na referência.</p>}
<SelectField name="kind" label="Tipo de plantio" required value={draft.kind} change={value => change('kind', value)} options={[{ id: 'Initial', label: 'Plantio inicial' }, { id: 'Replacement', label: 'Reposição de plantas' }]} error={editor.error} />
{draft.kind === 'Replacement' && <SelectField name="replacedPlantingId" label="Plantio que está sendo reposto" required value={draft.replacedPlantingId} change={value => change('replacedPlantingId', value)} options={options.data?.planting?.filter(x => x.cycleId === draft.cycleId) ?? []} error={editor.error} />}
<details className="organization-disclosure"><summary>Origem, espaçamento e responsável (opcional)</summary><div className="grid gap-5">
<SelectField name="propagationLotId" label="Lote de origem" value={draft.propagationLotId} change={value => change('propagationLotId', value)} options={options.data?.propagationLot?.filter(x => x.speciesId === fixedCycle?.speciesId) ?? []} error={editor.error} />
<Field name="plantSpacingCm" label="Espaçamento entre plantas (cm)" type="decimal" value={draft.plantSpacingCm} change={value => change('plantSpacingCm', value)} error={editor.error} />
<Field name="rowSpacingCm" label="Espaçamento entre linhas (cm)" type="decimal" value={draft.rowSpacingCm} change={value => change('rowSpacingCm', value)} error={editor.error} />
<SelectField name="responsibleParticipantId" label="Responsável pelo trabalho" value={draft.responsibleParticipantId} change={value => change('responsibleParticipantId', value)} options={options.data?.participant ?? []} error={editor.error} />
</div></details></div> : <div className="form-stack">
<Group title="O que foi plantado">
{!fixedCycle && <SelectField name="cycleId" label="Ciclo" required value={draft.cycleId} change={value => change("cycleId", value)} options={options.data?.cultivationCycle ?? []} error={editor.error} />}
<SelectField name="kind" label="Tipo de plantio" required value={draft.kind} change={value => change("kind", value)} options={[{ id: 'Initial', label: enumLabels['Initial'] }, { id: 'Replacement', label: enumLabels['Replacement'] }]} error={editor.error} />
{draft.kind === "Replacement" && <SelectField name="replacedPlantingId" label="Plantio que está sendo reposto" value={draft.replacedPlantingId} change={value => change("replacedPlantingId", value)} options={options.data?.planting?.filter(x => x.cycleId === draft.cycleId && x.id !== original?.id) ?? []} error={editor.error} />}
<SelectField name="propagationLotId" label="Lote de origem" value={draft.propagationLotId} change={value => change("propagationLotId", value)} options={options.data?.propagationLot?.filter(x => x.speciesId === options.data?.cultivationCycle?.find(c => c.id === draft.cycleId)?.speciesId) ?? []} error={editor.error} />
</Group>
<Group title="Plantio">
<Field name="plantedOn" label="Data do plantio" required type="date" value={draft.plantedOn} change={value => change("plantedOn", value)} error={editor.error} />
<Field name="plantedQuantity" label="Quantidade plantada" required type="integer" value={draft.plantedQuantity} change={value => change("plantedQuantity", value)} error={editor.error} />
<SelectField name="responsibleParticipantId" label="Responsável pelo trabalho" value={draft.responsibleParticipantId} change={value => change("responsibleParticipantId", value)} options={options.data?.participant ?? []} error={editor.error} />
</Group>
<Group title="Espaçamento" hint="Opcionais.">
<Field name="plantSpacingCm" label="Entre plantas" type="decimal" value={draft.plantSpacingCm} change={value => change("plantSpacingCm", value)} error={editor.error} unit="cm" />
<Field name="rowSpacingCm" label="Entre linhas" type="decimal" value={draft.rowSpacingCm} change={value => change("rowSpacingCm", value)} error={editor.error} unit="cm" />
</Group>
</div>}</EditorFrame>
}
export function PlantingReview({ record }: { record: Planting }) { const options = useLookups(record.gardenId, ['cultivationCycle', 'participant', 'planting', 'propagationLot']); return <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[{ label: 'Ciclo', value: record.cycleId ? <a className="underline" href={linkTo(readRoute().garden, 'cultivo/ciclos', record.cycleId)}>{options.data?.cultivationCycle?.find(x => x.id === record.cycleId)?.label ?? record.cycleId}</a> : "Não informado" }, { label: 'Lote de origem', value: record.propagationLotId ? <a className="underline" href={linkTo(readRoute().garden, 'cultivo/origens', record.propagationLotId)}>{options.data?.propagationLot?.find(x => x.id === record.propagationLotId)?.label ?? record.propagationLotId}</a> : "Não informado" }, { label: 'Plantio original da reposição', value: record.replacedPlantingId ? <a className="underline" href={linkTo(readRoute().garden, 'cultivo/plantios', record.replacedPlantingId)}>{options.data?.planting?.find(x => x.id === record.replacedPlantingId)?.label ?? record.replacedPlantingId}</a> : "Não informado" }, { label: 'Data do plantio', value: record.plantedOn ? record.plantedOn.split("-").reverse().join("/") : "Não informado" }, { label: 'Natureza do plantio', value: enumLabels[record.kind] }, { label: 'Quantidade plantada', value: record.plantedQuantity === null ? "Não informado" : formatNumber(record.plantedQuantity) }, { label: 'Espaçamento entre plantas (cm)', value: record.plantSpacingCm === null ? "Não informado" : formatNumber(record.plantSpacingCm) }, { label: 'Espaçamento entre linhas (cm)', value: record.rowSpacingCm === null ? "Não informado" : formatNumber(record.rowSpacingCm) }, { label: 'Responsável pelo trabalho', value: record.responsibleParticipantId ? <a className="underline" href={linkTo(readRoute().garden, 'participantes', record.responsibleParticipantId)}>{options.data?.participant?.find(x => x.id === record.responsibleParticipantId)?.label ?? record.responsibleParticipantId}</a> : "Não informado" }]} /></> }
