import { routeHref } from "../../../hooks/useRoute"
import { readRoute } from "../../../hooks/useRoute"
import { useState } from "react"
import { EditorFrame } from "../../../components/EditorFrame"
import { Field, SelectField, Facts, Group } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useEditor } from "../../../hooks/useEditor"
import { useLookups } from "../../../hooks/useLookups"
import { samplingPointApi } from "../services/SamplingPointApi"
import type { SamplingPoint, SamplingPointValues } from "../types/SamplingPoint"
import { optionalText } from "../../../services/values"
export function SamplingPointForm({ garden, original, saved, cancel }: { garden: string; original?: SamplingPoint; saved: (record: SamplingPoint) => void; cancel: () => void }) {
const editor = useEditor<SamplingPoint, SamplingPointValues>(original, (values) => samplingPointApi.create(garden, values), (values, version, reason) => { if (!original) throw new Error("Registro indisponível"); return samplingPointApi.correct(garden, original.id, values, version, reason) }, () => { if (!original) throw new Error("Registro indisponível"); return samplingPointApi.get(garden, original.id) }, saved, false)
const [draft, setDraft] = useState<{ description: string; location: string; bedId: string; sectorId: string }>(() => ({
description: original ? original.description : '',
location: original ? original.location ?? "" : '',
bedId: original ? original.bedId ?? "" : readRoute().search.get('bedId') || '',
sectorId: original ? original.sectorId ?? "" : ''
}))
const options = useLookups(garden, ['bed', 'irrigationSector'])
function change<K extends keyof typeof draft>(key: K, value: typeof draft[K]) { setDraft(current => ({ ...current, [key]: value, ...(key === 'bedId' ? { sectorId: '' } : {}), ...(key === 'sectorId' ? { bedId: '' } : {}) })) }
function values(): SamplingPointValues { if (options.loading || options.error) throw new Error("Aguarde ou recarregue os cadastros de apoio."); return {
description: draft.description.trim(),
location: optionalText(draft.location),
bedId: optionalText(draft.bedId),
sectorId: optionalText(draft.sectorId),
} }
return <EditorFrame dependenciesReady={!options.loading && !options.error} title={original ? "Corrigir ponto de coleta" : "Novo ponto de coleta"} busy={editor.busy} uncertain={!!editor.attempt || editor.catalogUncertain} catalogUncertain={editor.catalogUncertain} error={editor.error} reason={original ? editor.reason : undefined} changeReason={original ? editor.setReason : undefined} submit={() => void editor.submit(values)} cancel={cancel} conflict={original ? () => void editor.reloadConflict() : undefined} review={editor.review && <Facts items={[{ label: "Versão", value: editor.review.version }, { label: "Valores atuais", value: <SamplingPointReview record={editor.review} /> }]} />} acceptReview={editor.acceptReview}>
{options.loading && <p role="status">Carregando cadastros de apoio…</p>}{options.error && <Failure error={options.error} retry={options.retry} />}
<div className="grid min-w-0 gap-5">
<Group title="Ponto de coleta">
<Field name="description" label="Descrição" required type="textarea" value={draft.description} change={value => change("description", value)} error={editor.error} />
<Field name="location" label="Localização" type="text" value={draft.location} change={value => change("location", value)} error={editor.error} />
</Group>
<Group title="Vínculos" hint="Opcionais. Um ponto geral não exige setor ou canteiro.">
<SelectField name="bedId" label="Canteiro" value={draft.bedId} change={value => change("bedId", value)} options={options.data?.bed ?? []} error={editor.error} />
<SelectField name="sectorId" label="Setor" value={draft.sectorId} change={value => change("sectorId", value)} options={options.data?.irrigationSector ?? []} error={editor.error} />
</Group>
</div></EditorFrame>
}
export function SamplingPointReview({ record }: { record: SamplingPoint }) { const options = useLookups(record.gardenId, ['bed', 'irrigationSector']); return <>{options.error && <Failure error={options.error} retry={options.retry} />}<Facts items={[{ label: 'Descrição', value: record.description }, { label: 'Localização', value: record.location }, { label: 'Canteiro', value: record.bedId ? <a className="underline" href={routeHref(readRoute().garden, 'horta', 'canteiros', record.bedId)}>{options.data?.bed?.find(x => x.id === record.bedId)?.label ?? record.bedId}</a> : "Não informado" }, { label: 'Setor', value: record.sectorId ? <a className="underline" href={routeHref(readRoute().garden, 'horta', 'setores', record.sectorId)}>{options.data?.irrigationSector?.find(x => x.id === record.sectorId)?.label ?? record.sectorId}</a> : "Não informado" }]} /></> }
