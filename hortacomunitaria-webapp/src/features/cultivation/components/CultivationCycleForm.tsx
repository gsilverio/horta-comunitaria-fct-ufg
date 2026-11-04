import type { Species } from '../types/Species'
import type { Bed } from '../../organization/types/Bed'
import { SpeciesForm } from './SpeciesForm'
import { useCallback } from 'react'
import { useRemote } from '../../../hooks/useRemote'
import { bedContextApi } from '../../organization/services/bedContextApi'
import { routeHref } from "../../../hooks/useRoute"
import { readRoute } from "../../../hooks/useRoute"
import { useState } from "react"
import { EditorFrame } from "../../../components/EditorFrame"
import { Field, SelectField, Facts } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useEditor } from "../../../hooks/useEditor"
import { useLookups } from "../../../hooks/useLookups"
import { cultivationCycleApi } from "../services/CultivationCycleApi"
import type { CultivationCycle, CultivationCycleValues } from "../types/CultivationCycle"
import { today, formatNumber } from "../../irrigation/formValues"
import { numberDraft, requiredNumber, optionalText } from "../../../services/values"
export function CultivationCycleForm({ garden, original, saved, cancel, fixedBed, fixedSpecies }: { fixedSpecies?: Species; fixedBed?: Bed; garden: string; original?: CultivationCycle; saved: (record: CultivationCycle) => void; cancel: () => void }) {
const editor = useEditor<CultivationCycle, CultivationCycleValues>(original, (values, key) => cultivationCycleApi.create(garden, values, key), (values, version, reason) => { if (!original) throw new Error("Registro indisponível"); return cultivationCycleApi.correct(garden, original.id, values, version, reason) }, () => { if (!original) throw new Error("Registro indisponível"); return cultivationCycleApi.get(garden, original.id) }, saved, true)
const [draft, setDraft] = useState<{ bedId: string; speciesId: string; cultivarId: string; startedOn: string; occupiedAreaM2: string; endedOn: string; responsibleParticipantId: string }>(() => ({
bedId: original ? original.bedId : fixedBed?.id ?? readRoute().search.get('bedId') ?? '',
speciesId: original ? original.speciesId : fixedSpecies?.id ?? readRoute().search.get('speciesId') ?? '',
cultivarId: original ? original.cultivarId ?? "" : '',
startedOn: original ? original.startedOn : today(),
occupiedAreaM2: original ? numberDraft(original.occupiedAreaM2) : '',
endedOn: original ? original.endedOn ?? "" : '',
responsibleParticipantId: original ? original.responsibleParticipantId ?? "" : ''
}))
const [addingSpecies, setAddingSpecies] = useState(false)
const options = useLookups(garden, ['bed', 'cultivar', 'participant', 'species'])
const bedContextLoad = useCallback((signal: AbortSignal) => draft.bedId && draft.startedOn ? bedContextApi.on(garden, draft.bedId, draft.startedOn, signal) : Promise.resolve(undefined), [garden, draft.bedId, draft.startedOn])
const bedContext = useRemote(`${garden}/${draft.bedId}/${draft.startedOn}`, bedContextLoad)

function change<K extends keyof typeof draft>(key: K, value: typeof draft[K]) { setDraft(current => ({ ...current, [key]: value, ...(key === 'speciesId' ? { cultivarId: '' } : {}) })) }
function values(): CultivationCycleValues { if (options.loading || options.error) throw new Error("Aguarde ou recarregue os cadastros de apoio."); return {
bedId: draft.bedId,
speciesId: draft.speciesId,
cultivarId: optionalText(draft.cultivarId),
startedOn: draft.startedOn,
occupiedAreaM2: requiredNumber(draft.occupiedAreaM2, 'occupiedAreaM2', 3, false),
endedOn: optionalText(draft.endedOn),
responsibleParticipantId: optionalText(draft.responsibleParticipantId),
} }
if (addingSpecies) return <SpeciesForm garden={garden} saved={species => { setDraft(current => ({ ...current, speciesId: species.id, cultivarId: '' })); options.retry(); setAddingSpecies(false) }} cancel={() => { options.retry(); setAddingSpecies(false) }} />
return <EditorFrame dependenciesReady={!options.loading && !options.error && !bedContext.loading && !bedContext.error && (!draft.bedId || !!bedContext.data?.configuration)} title={original ? "Corrigir: Ciclo de cultivo" : fixedBed ? "O que será plantado?" : "Novo: Ciclo de cultivo"} submitLabel={fixedBed ? "Cadastrar cultivo" : "Salvar registro"} description={fixedBed ? `Escolha a planta e informe quando e em qual área será cultivada em ${fixedBed.name}. Depois, você pode registrar a quantidade plantada.` : undefined} busy={editor.busy} uncertain={!!editor.attempt || editor.catalogUncertain} catalogUncertain={editor.catalogUncertain} error={editor.error} reason={original ? editor.reason : undefined} changeReason={original ? editor.setReason : undefined} submit={() => void editor.submit(values)} cancel={cancel} conflict={original ? () => void editor.reloadConflict() : undefined} review={editor.review && <Facts items={[{ label: "Versão", value: editor.review.version }, { label: "Valores atuais", value: <CultivationCycleReview record={editor.review} /> }]} />} acceptReview={editor.acceptReview}>
{options.loading && <p role="status">Carregando cadastros de apoio…</p>}{options.error && <Failure error={options.error} retry={options.retry} />}
{bedContext.error && <Failure error={bedContext.error} />}
{draft.bedId && !bedContext.loading && !bedContext.error && !bedContext.data?.configuration && <p className="notice">Este canteiro não tem área configurada na data de início. <a className="underline" href={routeHref(garden, 'horta', 'canteiros', draft.bedId)}>Configurar área do canteiro</a>. Revise a data ou conclua a preparação antes de abrir o ciclo.</p>}
{fixedBed ? <div className="grid gap-5">
{fixedSpecies ? <p className="notice"><strong>{fixedSpecies.commonName}</strong> · espécie escolhida no catálogo</p> : <SelectField name="speciesId" label="O que será plantado" required value={draft.speciesId} change={value => change('speciesId', value)} options={options.data?.species ?? []} error={editor.error} />}
{!fixedSpecies && <div><button type="button" className="secondary" disabled={editor.busy || !!editor.attempt} onClick={() => setAddingSpecies(true)}>Cadastrar espécie</button><p className="hint mt-2">Não encontrou a planta? Cadastre aqui e continue neste canteiro.</p></div>}
<Field name="startedOn" label="Data de início" required type="date" value={draft.startedOn} change={value => change('startedOn', value)} error={editor.error} />
<Field name="occupiedAreaM2" label="Área ocupada (m²)" required type="decimal" value={draft.occupiedAreaM2} change={value => change('occupiedAreaM2', value)} error={editor.error} hint="Informe a área destinada a esta planta. Um canteiro pode ter mais de um cultivo." />
<details className="organization-disclosure"><summary>Variedade e responsável (opcional)</summary><div className="grid gap-5">
<SelectField name="cultivarId" label="Variedade" value={draft.cultivarId} change={value => change('cultivarId', value)} options={options.data?.cultivar?.filter(x => x.speciesId === draft.speciesId) ?? []} error={editor.error} />
<SelectField name="responsibleParticipantId" label="Responsável pelo trabalho" value={draft.responsibleParticipantId} change={value => change('responsibleParticipantId', value)} options={options.data?.participant ?? []} error={editor.error} />
</div></details></div> : <div className="grid min-w-0 gap-4 sm:grid-cols-2">
{!fixedBed && <SelectField name="bedId" label="Canteiro" required value={draft.bedId} change={value => change("bedId", value)} options={options.data?.bed ?? []} error={editor.error} />}
<SelectField name="speciesId" label="Espécie" required value={draft.speciesId} change={value => change("speciesId", value)} options={options.data?.species ?? []} error={editor.error} />
{fixedBed && <button type="button" className="secondary self-end" disabled={editor.busy || !!editor.attempt} onClick={() => setAddingSpecies(true)}>Cadastrar espécie</button>}
<SelectField name="cultivarId" label="Cultivar" value={draft.cultivarId} change={value => change("cultivarId", value)} options={options.data?.cultivar?.filter(x => x.speciesId === draft.speciesId) ?? []} error={editor.error} />
<Field name="startedOn" label="Data de início" required type="date" value={draft.startedOn} change={value => change("startedOn", value)} error={editor.error} />
<Field name="occupiedAreaM2" label="Área ocupada (m²)" required type="decimal" value={draft.occupiedAreaM2} change={value => change("occupiedAreaM2", value)} error={editor.error} />
{original && <Field name="endedOn" label="Data de encerramento" type="date" value={draft.endedOn} change={value => change("endedOn", value)} error={editor.error} />}
<SelectField name="responsibleParticipantId" label="Responsável pelo trabalho" value={draft.responsibleParticipantId} change={value => change("responsibleParticipantId", value)} options={options.data?.participant ?? []} error={editor.error} />
</div>}</EditorFrame>
}
export function CultivationCycleReview({ record }: { record: CultivationCycle }) { const options = useLookups(record.gardenId, ['bed', 'cultivar', 'participant', 'species']); return <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[{ label: 'Canteiro', value: record.bedId ? <a className="underline" href={routeHref(readRoute().garden, 'horta', 'canteiros', record.bedId)}>{options.data?.bed?.find(x => x.id === record.bedId)?.label ?? record.bedId}</a> : "Não informado" }, { label: 'Espécie', value: record.speciesId ? <a className="underline" href={routeHref(readRoute().garden, 'cultivo', 'especies', record.speciesId)}>{options.data?.species?.find(x => x.id === record.speciesId)?.label ?? record.speciesId}</a> : "Não informado" }, { label: 'Cultivar', value: record.cultivarId ? <a className="underline" href={routeHref(readRoute().garden, 'cultivo', 'cultivares', record.cultivarId, new URLSearchParams({ speciesId: record.speciesId }))}>{options.data?.cultivar?.find(x => x.id === record.cultivarId)?.label ?? record.cultivarId}</a> : "Não informado" }, { label: 'Data de início', value: record.startedOn ? record.startedOn.split("-").reverse().join("/") : "Não informado" }, { label: 'Área ocupada (m²)', value: record.occupiedAreaM2 === null ? "Não informado" : formatNumber(record.occupiedAreaM2) }, { label: 'Data de encerramento', value: record.endedOn ? record.endedOn.split("-").reverse().join("/") : "Não informado" }, { label: 'Responsável pelo trabalho', value: record.responsibleParticipantId ? <a className="underline" href={routeHref(readRoute().garden, 'atividades', 'participantes', record.responsibleParticipantId)}>{options.data?.participant?.find(x => x.id === record.responsibleParticipantId)?.label ?? record.responsibleParticipantId}</a> : "Não informado" }]} /></> }
