import { readRoute } from '../../../routes/navigation'
import { linkTo } from '../../../routes/routes'
import { useState } from "react"
import { EditorFrame } from "../../../components/EditorFrame"
import { Field, SelectField, Facts, Group } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useEditor } from "../../../hooks/useEditor"
import { useLookups } from "../../../hooks/useLookups"
import { occurrenceApi } from "../services/OccurrenceApi"
import type { Occurrence, OccurrenceValues } from "../types/Occurrence"
import { today, localInstant, formatInstant } from "../../irrigation/formValues"
import { optionalText, instant } from "../../../services/values"
import { enumValue } from "../../../types/enums"
import type { OccurrenceStatus } from "../../../types/enums"
import { enumLabels } from "../../../services/labels"
export function OccurrenceForm({ garden, original, saved, cancel }: { garden: string; original?: Occurrence; saved: (record: Occurrence) => void; cancel: () => void }) {
const editor = useEditor<Occurrence, OccurrenceValues>(original, (values, key) => occurrenceApi.create(garden, values, key), (values, version, reason) => { if (!original) throw new Error("Registro indisponível"); return occurrenceApi.correct(garden, original.id, values, version, reason) }, () => { if (!original) throw new Error("Registro indisponível"); return occurrenceApi.get(garden, original.id) }, saved, true)
const [draft, setDraft] = useState<{ openedAt: string; category: string; description: string; status: string; resolvedAt: string; resolution: string; responsibleParticipantId: string; bedId: string; sectorId: string; cycleId: string; irrigationId: string }>(() => ({
openedAt: original ? (original.openedAt ? localInstant(original.openedAt) : "") : today() + "T08:00",
category: original ? original.category : '',
description: original ? original.description : '',
status: original ? original.status : 'Open',
resolvedAt: original ? (original.resolvedAt ? localInstant(original.resolvedAt) : "") : '',
resolution: original ? original.resolution ?? "" : '',
responsibleParticipantId: original ? original.responsibleParticipantId ?? "" : '',
bedId: original ? original.bedId ?? "" : readRoute().search.get('bedId') || '',
sectorId: original ? original.sectorId ?? "" : '',
cycleId: original ? original.cycleId ?? "" : readRoute().search.get('cycleId') || '',
irrigationId: original ? original.irrigationId ?? "" : ''
}))
const options = useLookups(garden, ['bed', 'cultivationCycle', 'irrigation', 'irrigationSector', 'participant'])
function change<K extends keyof typeof draft>(key: K, value: typeof draft[K]) { setDraft(current => ({ ...current, [key]: value, ...(key === 'bedId' ? { sectorId: '', cycleId: '', irrigationId: '' } : {}), ...(key === 'sectorId' ? { bedId: '', cycleId: '', irrigationId: '' } : {}), ...(key === 'cycleId' ? { bedId: '', sectorId: '', irrigationId: '' } : {}), ...(key === 'irrigationId' ? { bedId: '', sectorId: '', cycleId: '' } : {}) })) }
function values(): OccurrenceValues { if (options.loading || options.error) throw new Error("Aguarde ou recarregue os cadastros de apoio."); return {
openedAt: instant(draft.openedAt, 'openedAt', original?.openedAt),
category: draft.category.trim(),
description: draft.description.trim(),
status: enumValue<OccurrenceStatus>(draft.status, ['Open', 'InProgress', 'Resolved']),
resolvedAt: draft.resolvedAt ? instant(draft.resolvedAt, 'resolvedAt', original?.resolvedAt) : null,
resolution: optionalText(draft.resolution),
responsibleParticipantId: optionalText(draft.responsibleParticipantId),
bedId: optionalText(draft.bedId),
sectorId: optionalText(draft.sectorId),
cycleId: optionalText(draft.cycleId),
irrigationId: optionalText(draft.irrigationId),
} }
return <EditorFrame dependenciesReady={!options.loading && !options.error} title={original ? "Corrigir ocorrência" : "Nova ocorrência"} busy={editor.busy} uncertain={!!editor.attempt || editor.catalogUncertain} catalogUncertain={editor.catalogUncertain} error={editor.error} reason={original ? editor.reason : undefined} changeReason={original ? editor.setReason : undefined} submit={() => void editor.submit(values)} cancel={cancel} conflict={original ? () => void editor.reloadConflict() : undefined} review={editor.review && <Facts items={[{ label: "Versão", value: editor.review.version }, { label: "Valores atuais", value: <OccurrenceReview record={editor.review} /> }]} />} acceptReview={editor.acceptReview}>
{options.loading && <p role="status">Carregando cadastros de apoio…</p>}{options.error && <Failure error={options.error} retry={options.retry} />}
<div className="grid min-w-0 gap-5">
<Group title="Ocorrência">
<Field name="openedAt" label="Abertura em" required type="datetime-local" value={draft.openedAt} change={value => change("openedAt", value)} error={editor.error} />
<Field name="category" label="Categoria" required type="text" value={draft.category} change={value => change("category", value)} error={editor.error} suggestions={["Falha de irrigação", "Vazamento", "Ataque de pragas", "Dano por animais", "Evento climático", "Furto ou vandalismo", "Falta de insumo", "Equipamento danificado", "Outro"]} hint="Escolha uma sugestão ou digite outra categoria." />
<Field name="description" label="Descrição" required type="textarea" value={draft.description} change={value => change("description", value)} error={editor.error} />
<SelectField name="responsibleParticipantId" label="Responsável pelo trabalho" value={draft.responsibleParticipantId} change={value => change("responsibleParticipantId", value)} options={options.data?.participant ?? []} error={editor.error} />
</Group>
<Group title="Local e vínculos" hint="Opcionais. A ocorrência pode ser geral.">
<SelectField name="bedId" label="Canteiro" value={draft.bedId} change={value => change("bedId", value)} options={options.data?.bed ?? []} error={editor.error} />
<SelectField name="sectorId" label="Setor" value={draft.sectorId} change={value => change("sectorId", value)} options={options.data?.irrigationSector ?? []} error={editor.error} />
<SelectField name="cycleId" label="Ciclo" value={draft.cycleId} change={value => change("cycleId", value)} options={options.data?.cultivationCycle ?? []} error={editor.error} />
<SelectField name="irrigationId" label="Irrigação" value={draft.irrigationId} change={value => change("irrigationId", value)} options={options.data?.irrigation ?? []} error={editor.error} />
</Group>
{original && <Group title="Acompanhamento">
{original && <SelectField name="status" label="Situação" required value={draft.status} change={value => change("status", value)} options={[{ id: 'Open', label: enumLabels['Open'] }, { id: 'InProgress', label: enumLabels['InProgress'] }, { id: 'Resolved', label: enumLabels['Resolved'] }]} error={editor.error} />}
{original && <Field name="resolvedAt" label="Resolução em" type="datetime-local" value={draft.resolvedAt} change={value => change("resolvedAt", value)} error={editor.error} />}
{original && <Field name="resolution" label="Descrição da resolução" type="textarea" value={draft.resolution} change={value => change("resolution", value)} error={editor.error} />}
</Group>}
</div></EditorFrame>
}
export function OccurrenceReview({ record }: { record: Occurrence }) { const options = useLookups(record.gardenId, ['bed', 'cultivationCycle', 'irrigation', 'irrigationSector', 'participant']); return <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[{ label: 'Abertura em', value: record.openedAt ? formatInstant(record.openedAt) : "Não informado" }, { label: 'Categoria', value: record.category }, { label: 'Descrição', value: record.description }, { label: 'Situação', value: enumLabels[record.status] }, { label: 'Resolução em', value: record.resolvedAt ? formatInstant(record.resolvedAt) : "Não informado" }, { label: 'Descrição da resolução', value: record.resolution }, { label: 'Responsável pelo trabalho', value: record.responsibleParticipantId ? <a className="underline" href={linkTo(readRoute().garden, 'atividades/participantes', record.responsibleParticipantId)}>{options.data?.participant?.find(x => x.id === record.responsibleParticipantId)?.label ?? record.responsibleParticipantId}</a> : "Não informado" }, { label: 'Canteiro', value: record.bedId ? <a className="underline" href={linkTo(readRoute().garden, 'horta/canteiros', record.bedId)}>{options.data?.bed?.find(x => x.id === record.bedId)?.label ?? record.bedId}</a> : "Não informado" }, { label: 'Setor', value: record.sectorId ? <a className="underline" href={linkTo(readRoute().garden, 'horta/setores', record.sectorId)}>{options.data?.irrigationSector?.find(x => x.id === record.sectorId)?.label ?? record.sectorId}</a> : "Não informado" }, { label: 'Ciclo', value: record.cycleId ? <a className="underline" href={linkTo(readRoute().garden, 'cultivo/ciclos', record.cycleId)}>{options.data?.cultivationCycle?.find(x => x.id === record.cycleId)?.label ?? record.cycleId}</a> : "Não informado" }, { label: 'Irrigação', value: options.data?.irrigation?.find(x => x.id === record.irrigationId)?.label ?? record.irrigationId }]} /></> }
