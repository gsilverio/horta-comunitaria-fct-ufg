import { routeHref } from "../../../hooks/useRoute"
import { readRoute } from "../../../hooks/useRoute"
import { useState } from "react"
import { EditorFrame } from "../../../components/EditorFrame"
import { Field, SelectField, Toggle, Group, Facts } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useEditor } from "../../../hooks/useEditor"
import { useLookups } from "../../../hooks/useLookups"
import { activityApi } from "../services/ActivityApi"
import type { Activity, ActivityValues } from "../types/Activity"
import { today, formatNumber } from "../../irrigation/formValues"
import { numberDraft, requiredNumber, numeric, optionalText } from "../../../services/values"
export function ActivityForm({ garden, original, saved, cancel }: { garden: string; original?: Activity; saved: (record: Activity) => void; cancel: () => void }) {
const editor = useEditor<Activity, ActivityValues>(original, (values, key) => activityApi.create(garden, values, key), (values, version, reason) => { if (!original) throw new Error("Registro indisponível"); return activityApi.correct(garden, original.id, values, version, reason) }, () => { if (!original) throw new Error("Registro indisponível"); return activityApi.get(garden, original.id) }, saved, true)
const [draft, setDraft] = useState<{ title: string; category: string; performedOn: string; description: string; bedId: string; sectorId: string; cycleId: string; occurrenceId: string; isOperational: boolean; isSocial: boolean; isEducational: boolean; objective: string; modality: string; reportedAudienceTotal: string; audienceListComplete: string; participants: { participantId: string; role: string; workHours: string; countsAsAudience: boolean }[]; inputUsages: { inputId: string; quantity: string; unit: string }[]; institutions: { institutionId: string; role: string }[] }>(() => ({
title: original ? original.title : '',
category: original ? original.category : '',
performedOn: original ? original.performedOn : today(),
description: original ? original.description ?? "" : '',
bedId: original ? original.bedId ?? "" : readRoute().search.get('bedId') || '',
sectorId: original ? original.sectorId ?? "" : '',
cycleId: original ? original.cycleId ?? "" : readRoute().search.get('cycleId') || '',
occurrenceId: original ? original.occurrenceId ?? "" : readRoute().search.get('occurrenceId') || '',
isOperational: original ? original.isOperational : true,
isSocial: original ? original.isSocial : false,
isEducational: original ? original.isEducational : false,
objective: original ? original.objective ?? "" : '',
modality: original ? original.modality ?? "" : '',
reportedAudienceTotal: original ? numberDraft(original.reportedAudienceTotal) : '',
audienceListComplete: original ? original.audienceListComplete === null ? '' : original.audienceListComplete ? 'true' : 'false' : '',
participants: original ? original.participants.map(item => ({ ...item, workHours: numberDraft(item.workHours), role: item.role ?? "" })) : [],
inputUsages: original ? original.inputUsages.map(item => ({ ...item, quantity: numberDraft(item.quantity) })) : [],
institutions: original ? original.institutions.map(item => ({ ...item, role: item.role ?? "" })) : []
}))
const options = useLookups(garden, ['agriculturalInput', 'bed', 'cultivationCycle', 'institution', 'irrigationSector', 'occurrence', 'participant'])
function change<K extends keyof typeof draft>(key: K, value: typeof draft[K]) { setDraft(current => ({ ...current, [key]: value, ...(key === 'bedId' ? { sectorId: '', cycleId: '' } : {}), ...(key === 'sectorId' ? { bedId: '', cycleId: '' } : {}), ...(key === 'cycleId' ? { bedId: '', sectorId: '' } : {}) })) }
function values(): ActivityValues { if (options.loading || options.error) throw new Error("Aguarde ou recarregue os cadastros de apoio."); return {
title: draft.title.trim(),
category: draft.category.trim(),
performedOn: draft.performedOn,
description: optionalText(draft.description),
bedId: optionalText(draft.bedId),
sectorId: optionalText(draft.sectorId),
cycleId: optionalText(draft.cycleId),
occurrenceId: optionalText(draft.occurrenceId),
isOperational: draft.isOperational,
isSocial: draft.isSocial,
isEducational: draft.isEducational,
objective: optionalText(draft.objective),
modality: optionalText(draft.modality),
reportedAudienceTotal: numeric(draft.reportedAudienceTotal, 'reportedAudienceTotal', false, 0, false),
audienceListComplete: draft.audienceListComplete === '' ? null : draft.audienceListComplete === 'true',
participants: draft.participants.map((item, index) => ({ participantId: item.participantId, role: optionalText(item.role), workHours: numeric(item.workHours, `participants[${index}].workHours`), countsAsAudience: item.countsAsAudience })),
inputUsages: draft.inputUsages.map((item, index) => ({ inputId: item.inputId, quantity: requiredNumber(item.quantity, `inputUsages[${index}].quantity`), unit: item.unit })),
institutions: draft.institutions.map(item => ({ institutionId: item.institutionId, role: optionalText(item.role) })),
} }
return <EditorFrame dependenciesReady={!options.loading && !options.error} title={original ? "Corrigir: Atividade" : "Novo: Atividade"} busy={editor.busy} uncertain={!!editor.attempt || editor.catalogUncertain} catalogUncertain={editor.catalogUncertain} error={editor.error} reason={original ? editor.reason : undefined} changeReason={original ? editor.setReason : undefined} submit={() => void editor.submit(values)} cancel={cancel} conflict={original ? () => void editor.reloadConflict() : undefined} review={editor.review && <Facts items={[{ label: "Versão", value: editor.review.version }, { label: "Valores atuais", value: <ActivityReview record={editor.review} /> }]} />} acceptReview={editor.acceptReview}>
{options.loading && <p role="status">Carregando cadastros de apoio…</p>}{options.error && <Failure error={options.error} retry={options.retry} />}
<div className="grid min-w-0 gap-4 sm:grid-cols-2">
<h3 className="form-section-title">Identificação da atividade</h3>
<Field name="title" label="Título" required type="text" value={draft.title} change={value => change("title", value)} error={editor.error} />
<Field name="category" label="Categoria" required type="text" value={draft.category} change={value => change("category", value)} error={editor.error} />
<Field name="performedOn" label="Data da atividade" required type="date" value={draft.performedOn} change={value => change("performedOn", value)} error={editor.error} />
<Field name="description" label="Descrição" type="textarea" value={draft.description} change={value => change("description", value)} error={editor.error} />
<h3 className="form-section-title">Local e vínculos</h3>
<SelectField name="bedId" label="Canteiro" value={draft.bedId} change={value => change("bedId", value)} options={options.data?.bed ?? []} error={editor.error} />
<SelectField name="sectorId" label="Setor" value={draft.sectorId} change={value => change("sectorId", value)} options={options.data?.irrigationSector ?? []} error={editor.error} />
<SelectField name="cycleId" label="Ciclo" value={draft.cycleId} change={value => change("cycleId", value)} options={options.data?.cultivationCycle ?? []} error={editor.error} />
<SelectField name="occurrenceId" label="Ocorrência vinculada" value={draft.occurrenceId} change={value => change("occurrenceId", value)} options={options.data?.occurrence ?? []} error={editor.error} />
<h3 className="form-section-title">Finalidade e público</h3>
<Toggle label="Operacional" checked={draft.isOperational} change={value => change("isOperational", value)} />
<Toggle label="Social" checked={draft.isSocial} change={value => change("isSocial", value)} />
<Toggle label="Educacional" checked={draft.isEducational} change={value => change("isEducational", value)} />
<Field name="objective" label="Objetivo" type="textarea" value={draft.objective} change={value => change("objective", value)} error={editor.error} />
<Field name="modality" label="Modalidade" type="text" value={draft.modality} change={value => change("modality", value)} error={editor.error} />
<Field name="reportedAudienceTotal" label="Público total (inclui nominais)" type="integer" value={draft.reportedAudienceTotal} change={value => change("reportedAudienceTotal", value)} error={editor.error} />
<SelectField name="audienceListComplete" label="Cobertura da lista nominal" value={draft.audienceListComplete} change={value => change("audienceListComplete", value)} options={[{ id: "true", label: "Completa" }, { id: "false", label: "Parcial" }]} error={editor.error} />
</div>
<section><h3>Participações</h3><p className="hint">Adicione pessoas conhecidas. Horas em branco permanecem não informadas.</p>{draft.participants.map((item, index) => <Group key={index} title={"Participações " + (index + 1)}>
<SelectField name={`participants[${index}].participantId`} label="Participante" required value={item.participantId} change={value => change("participants", draft.participants.map((row, i) => i === index ? { ...row, participantId: value } : row))} options={options.data?.participant ?? []} error={editor.error} />
<Field name={`participants[${index}].role`} label="Papel" type="text" value={item.role} change={value => change("participants", draft.participants.map((row, i) => i === index ? { ...row, role: value } : row))} error={editor.error} />
<Field name={`participants[${index}].workHours`} label="Horas individuais (h)" type="decimal" value={item.workHours} change={value => change("participants", draft.participants.map((row, i) => i === index ? { ...row, workHours: value } : row))} error={editor.error} />
<Toggle label="Conta como público" checked={item.countsAsAudience} change={value => change("participants", draft.participants.map((row, i) => i === index ? { ...row, countsAsAudience: value } : row))} />
<button type="button" className="danger-outline" onClick={() => change("participants", draft.participants.filter((_, i) => i !== index))}>Remover item {index + 1}</button></Group>)}
<button type="button" className="secondary mt-4" onClick={() => change("participants", [...draft.participants, { participantId: '', role: '', workHours: '', countsAsAudience: false }])}>Adicionar participação</button></section>

<section><h3>Usos de insumos</h3><p className="hint">Adicione somente vínculos e quantidades conhecidos.</p>{draft.inputUsages.map((item, index) => <Group key={index} title={"Usos de insumos " + (index + 1)}>
<SelectField name={`inputUsages[${index}].inputId`} label="Insumo" required value={item.inputId} change={value => change("inputUsages", draft.inputUsages.map((row, i) => i === index ? { ...row, inputId: value, unit: options.data?.agriculturalInput?.find(x => x.id === value)?.referenceUnit ?? "" } : row))} options={options.data?.agriculturalInput ?? []} error={editor.error} />
<Field name={`inputUsages[${index}].quantity`} label="Quantidade utilizada" required type="decimal" value={item.quantity} change={value => change("inputUsages", draft.inputUsages.map((row, i) => i === index ? { ...row, quantity: value } : row))} error={editor.error} />
<Field name={`inputUsages[${index}].unit`} label="Unidade" required type="text" value={item.unit} change={value => change("inputUsages", draft.inputUsages.map((row, i) => i === index ? { ...row, unit: value } : row))} error={editor.error} />
<button type="button" className="danger-outline" onClick={() => change("inputUsages", draft.inputUsages.filter((_, i) => i !== index))}>Remover item {index + 1}</button></Group>)}
<button type="button" className="secondary mt-4" onClick={() => change("inputUsages", [...draft.inputUsages, { inputId: '', quantity: '', unit: '' }])}>Adicionar uso de insumo</button></section>

<section><h3>Instituições</h3><p className="hint">Adicione somente vínculos e quantidades conhecidos.</p>{draft.institutions.map((item, index) => <Group key={index} title={"Instituições " + (index + 1)}>
<SelectField name={`institutions[${index}].institutionId`} label="Instituição" required value={item.institutionId} change={value => change("institutions", draft.institutions.map((row, i) => i === index ? { ...row, institutionId: value } : row))} options={options.data?.institution ?? []} error={editor.error} />
<Field name={`institutions[${index}].role`} label="Papel" type="text" value={item.role} change={value => change("institutions", draft.institutions.map((row, i) => i === index ? { ...row, role: value } : row))} error={editor.error} />
<button type="button" className="danger-outline" onClick={() => change("institutions", draft.institutions.filter((_, i) => i !== index))}>Remover item {index + 1}</button></Group>)}
<button type="button" className="secondary mt-4" onClick={() => change("institutions", [...draft.institutions, { institutionId: '', role: '' }])}>Adicionar instituição</button></section>
</EditorFrame>
}
export function ActivityReview({ record }: { record: Activity }) { const options = useLookups(record.gardenId, ['agriculturalInput', 'bed', 'cultivationCycle', 'institution', 'irrigationSector', 'occurrence', 'participant']); return <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[{ label: 'Título', value: record.title }, { label: 'Categoria', value: record.category }, { label: 'Data da atividade', value: record.performedOn ? record.performedOn.split("-").reverse().join("/") : "Não informado" }, { label: 'Descrição', value: record.description }, { label: 'Canteiro', value: record.bedId ? <a className="underline" href={routeHref(readRoute().garden, 'horta', 'canteiros', record.bedId)}>{options.data?.bed?.find(x => x.id === record.bedId)?.label ?? record.bedId}</a> : "Não informado" }, { label: 'Setor', value: record.sectorId ? <a className="underline" href={routeHref(readRoute().garden, 'horta', 'setores', record.sectorId)}>{options.data?.irrigationSector?.find(x => x.id === record.sectorId)?.label ?? record.sectorId}</a> : "Não informado" }, { label: 'Ciclo', value: record.cycleId ? <a className="underline" href={routeHref(readRoute().garden, 'cultivo', 'ciclos', record.cycleId)}>{options.data?.cultivationCycle?.find(x => x.id === record.cycleId)?.label ?? record.cycleId}</a> : "Não informado" }, { label: 'Ocorrência vinculada', value: record.occurrenceId ? <a className="underline" href={routeHref(readRoute().garden, 'ocorrencias', 'registros', record.occurrenceId)}>{options.data?.occurrence?.find(x => x.id === record.occurrenceId)?.label ?? record.occurrenceId}</a> : "Não informado" }, { label: 'Operacional', value: record.isOperational === null ? "Não informado" : record.isOperational ? "Sim" : "Não" }, { label: 'Social', value: record.isSocial === null ? "Não informado" : record.isSocial ? "Sim" : "Não" }, { label: 'Educacional', value: record.isEducational === null ? "Não informado" : record.isEducational ? "Sim" : "Não" }, { label: 'Objetivo', value: record.objective }, { label: 'Modalidade', value: record.modality }, { label: 'Público total (inclui nominais)', value: record.reportedAudienceTotal === null ? "Não informado" : formatNumber(record.reportedAudienceTotal) }, { label: 'Cobertura da lista nominal', value: record.audienceListComplete === null ? "Não informado" : record.audienceListComplete ? "Sim" : "Não" }, { label: 'Participações', value: <ul className="space-y-4">{record.participants.map((item, index) => <li className="rounded-lg border border-horta-line p-3" key={index}><Facts items={[{ label: 'Participante', value: options.data?.participant?.find(x => x.id === item.participantId)?.label ?? item.participantId }, { label: 'Papel', value: item.role }, { label: 'Horas individuais (h)', value: item.workHours === null ? "Não informado" : formatNumber(item.workHours) }, { label: 'Conta como público', value: item.countsAsAudience ? "Sim" : "Não" }]} /></li>)}</ul> }, { label: 'Usos de insumos', value: <ul className="space-y-4">{record.inputUsages.map((item, index) => <li className="rounded-lg border border-horta-line p-3" key={index}><Facts items={[{ label: 'Insumo', value: options.data?.agriculturalInput?.find(x => x.id === item.inputId)?.label ?? item.inputId }, { label: 'Quantidade utilizada', value: item.quantity === null ? "Não informado" : formatNumber(item.quantity) }, { label: 'Unidade', value: item.unit }]} /></li>)}</ul> }, { label: 'Instituições', value: <ul className="space-y-4">{record.institutions.map((item, index) => <li className="rounded-lg border border-horta-line p-3" key={index}><Facts items={[{ label: 'Instituição', value: options.data?.institution?.find(x => x.id === item.institutionId)?.label ?? item.institutionId }, { label: 'Papel', value: item.role }]} /></li>)}</ul> }]} /></> }
