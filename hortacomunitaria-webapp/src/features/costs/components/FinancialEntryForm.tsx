import type { DetailItem } from "../../../types/records"
import { readRoute } from '../../../routes/navigation'
import { linkTo } from '../../../routes/routes'
import { useState } from "react"
import { EditorFrame } from "../../../components/EditorFrame"
import { Field, SelectField, Facts, Group } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useEditor } from "../../../hooks/useEditor"
import { useLookups } from "../../../hooks/useLookups"
import { financialEntryApi } from "../services/FinancialEntryApi"
import type { FinancialEntry, FinancialEntryValues } from "../types/FinancialEntry"
import { today, formatNumber } from "../../irrigation/formValues"
import { numberDraft, requiredNumber, optionalText } from "../../../services/values"
import { enumValue } from "../../../types/enums"
import type { FinancialNature } from "../../../types/enums"
import { enumLabels } from "../../../services/labels"
export function FinancialEntryForm({ garden, original, saved, cancel }: { garden: string; original?: FinancialEntry; saved: (record: FinancialEntry) => void; cancel: () => void }) {
const editor = useEditor<FinancialEntry, FinancialEntryValues>(original, (values, key) => financialEntryApi.create(garden, values, key), (values, version, reason) => { if (!original) throw new Error("Registro indisponível"); return financialEntryApi.correct(garden, original.id, values, version, reason) }, () => { if (!original) throw new Error("Registro indisponível"); return financialEntryApi.get(garden, original.id) }, saved, true)
const [draft, setDraft] = useState<{ entryDate: string; description: string; category: string; amountBrl: string; nature: string; bedId: string; cycleId: string }>(() => ({
entryDate: original ? original.entryDate : today(),
description: original ? original.description : '',
category: original ? original.category : '',
amountBrl: original ? numberDraft(original.amountBrl) : '',
nature: original ? original.nature : 'Planned',
bedId: original ? original.bedId ?? "" : readRoute().search.get('bedId') || '',
cycleId: original ? original.cycleId ?? "" : readRoute().search.get('cycleId') || ''
}))
const options = useLookups(garden, ['bed', 'cultivationCycle'])
function change<K extends keyof typeof draft>(key: K, value: typeof draft[K]) { setDraft(current => ({ ...current, [key]: value, ...(key === 'bedId' ? { cycleId: '' } : {}), ...(key === 'cycleId' ? { bedId: '' } : {}) })) }
function values(): FinancialEntryValues { if (options.loading || options.error) throw new Error("Aguarde ou recarregue os cadastros de apoio."); return {
entryDate: draft.entryDate,
description: draft.description.trim(),
category: draft.category.trim(),
amountBrl: requiredNumber(draft.amountBrl, 'amountBrl', 2, false),
nature: enumValue<FinancialNature>(draft.nature, ['Planned', 'Actual']),
bedId: optionalText(draft.bedId),
cycleId: optionalText(draft.cycleId),
} }
return <EditorFrame dependenciesReady={!options.loading && !options.error} title={original ? "Corrigir lançamento financeiro" : "Novo lançamento financeiro"} busy={editor.busy} uncertain={!!editor.attempt || editor.catalogUncertain} catalogUncertain={editor.catalogUncertain} error={editor.error} reason={original ? editor.reason : undefined} changeReason={original ? editor.setReason : undefined} submit={() => void editor.submit(values)} cancel={cancel} conflict={original ? () => void editor.reloadConflict() : undefined} review={editor.review && <Facts items={[{ label: "Versão", value: editor.review.version }, { label: "Valores atuais", value: <FinancialEntryReview record={editor.review} /> }]} />} acceptReview={editor.acceptReview}>
{options.loading && <p role="status">Carregando cadastros de apoio…</p>}{options.error && <Failure error={options.error} retry={options.retry} />}
<div className="form-stack">
<Group title="Lançamento">
<Field name="entryDate" label="Data de competência" required type="date" value={draft.entryDate} change={value => change("entryDate", value)} error={editor.error} />
<SelectField name="nature" label="Natureza" required value={draft.nature} change={value => change("nature", value)} options={[{ id: 'Planned', label: enumLabels['Planned'] }, { id: 'Actual', label: enumLabels['Actual'] }]} error={editor.error} />
<Field name="category" label="Categoria" required type="text" value={draft.category} change={value => change("category", value)} error={editor.error} />
<Field name="amountBrl" label="Valor" required type="decimal" value={draft.amountBrl} change={value => change("amountBrl", value)} error={editor.error} unit="R$" />
</Group>
<Group title="Detalhes">
<Field name="description" label="Descrição" required type="textarea" value={draft.description} change={value => change("description", value)} error={editor.error} />
</Group>
<Group title="Vínculos" hint="Opcionais. Vazio significa custo geral da horta.">
<SelectField name="bedId" label="Canteiro" value={draft.bedId} change={value => change("bedId", value)} options={options.data?.bed ?? []} error={editor.error} />
<SelectField name="cycleId" label="Ciclo" value={draft.cycleId} change={value => change("cycleId", value)} options={options.data?.cultivationCycle ?? []} error={editor.error} />
</Group>
</div></EditorFrame>
}
export function FinancialEntryReview({ record, extra = [] }: { record: FinancialEntry; extra?: DetailItem[] }) { const options = useLookups(record.gardenId, ['bed', 'cultivationCycle']); return <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[{ label: 'Data de competência', value: record.entryDate ? record.entryDate.split("-").reverse().join("/") : "Não informado" }, { label: 'Descrição', value: record.description }, { label: 'Categoria', value: record.category }, { label: 'Valor (BRL)', value: record.amountBrl === null ? "Não informado" : formatNumber(record.amountBrl) }, { label: 'Natureza', value: enumLabels[record.nature] }, { label: 'Canteiro', value: record.bedId ? <a className="underline" href={linkTo(readRoute().garden, 'horta/canteiros', record.bedId)}>{options.data?.bed?.find(x => x.id === record.bedId)?.label ?? record.bedId}</a> : "Não informado" }, { label: 'Ciclo', value: record.cycleId ? <a className="underline" href={linkTo(readRoute().garden, 'cultivo/ciclos', record.cycleId)}>{options.data?.cultivationCycle?.find(x => x.id === record.cycleId)?.label ?? record.cycleId}</a> : "Não informado" }, ...extra]} /></> }
