import { useCallback, useState } from 'react'
import { EditorFrame } from '../../../components/EditorFrame'
import { Field, SelectField } from '../../../components/Fields'
import { Failure } from '../../../components/Feedback'
import { useRemote } from '../../../hooks/useRemote'
import { useLookups } from '../../../hooks/useLookups'
import { requiredNumber, numeric, optionalText } from '../../../services/values'
import { releaseNavigationGuard } from '../../../services/navigationGuard'
import { today, formatNumber } from '../../irrigation/formValues'
import { bedContextApi } from '../../organization/services/bedContextApi'
import type { Bed } from '../../organization/types/Bed'
import type { Species } from '../../cultivation/types/Species'
import { useInteractivePlanting } from '../hooks/useInteractivePlanting'

export function InteractivePlantingForm({ garden, bed, species, saved, cancel, resume }: { garden: string; bed: Bed; species: Species; saved: () => void; cancel: () => void; resume: (cycleId: string) => void }) {
  const [draft, setDraft] = useState({ on: today(), area: '', occupied: '', quantity: '', cultivar: '', origin: '', participant: '', plantSpacing: '', rowSpacing: '' })
  const [revision, setRevision] = useState(0)
  const options = useLookups(garden, ['cultivar', 'propagationLot', 'participant'])
  const load = useCallback((signal: AbortSignal) => bedContextApi.on(garden, bed.id, draft.on, signal), [garden, bed.id, draft.on])
  const context = useRemote(`${garden}/${bed.id}/${draft.on}/${revision}`, load)
  const command = useInteractivePlanting(garden, saved)
  const change = (key: keyof typeof draft, value: string) => setDraft(current => ({ ...current, [key]: value }))
  const ready = !context.loading && !context.error && !!context.data?.sectorAssignment && !options.loading && !options.error
  return <EditorFrame title="Preparar plantio" description="Data, área e quantidade do plantio inicial." submitLabel="Confirmar plantio" cancelLabel="Voltar às espécies" busy={command.busy} uncertain={command.pending} error={command.error} dependenciesReady={ready && !command.partialCycle} cancel={cancel} submit={() => void command.submit(() => ({ bedId: bed.id, speciesId: species.id, cultivarId: optionalText(draft.cultivar), on: draft.on, bedAreaM2: context.data?.configuration ? null : requiredNumber(draft.area, 'cultivableAreaM2', 3, false), occupiedAreaM2: requiredNumber(draft.occupied, 'occupiedAreaM2', 3, false), quantity: requiredNumber(draft.quantity, 'plantedQuantity', 0, false), responsibleParticipantId: optionalText(draft.participant), propagationLotId: optionalText(draft.origin), plantSpacingCm: numeric(draft.plantSpacing, 'plantSpacingCm', false, 3, false), rowSpacingCm: numeric(draft.rowSpacing, 'rowSpacingCm', false, 3, false) }))}>
    <div className="border-l-4 border-[#8fa668] bg-[#eaf0dc] px-3 py-2"><strong className="block text-sm">{species.commonName}</strong><span className="text-xs text-horta-muted">Plantio inicial · {bed.name}</span></div>
    <Field name="on" label="Data do plantio" required type="date" value={draft.on} change={value => change('on', value)} error={command.error} />
    {context.loading && <p role="status">Conferindo o canteiro nesta data…</p>}
    {context.error && <Failure error={context.error} retry={() => setRevision(value => value + 1)} />}
    {context.data && !context.data.sectorAssignment && <p className="notice">Não há setor vigente nesta data. Escolha uma data em que o canteiro esteja associado ao setor.</p>}
    {context.data && !context.data.configuration && <Field name="cultivableAreaM2" label="Área real do canteiro (m²)" type="decimal" required value={draft.area} change={value => change('area', value)} error={command.error} hint="Só é necessário informar na primeira configuração. O grid não representa metros." />}
    {context.data?.configuration && <p className="m-0 text-xs text-horta-muted">Área cultivável cadastrada: {formatNumber(context.data.configuration.cultivableAreaM2)} m².</p>}
    <div className="grid gap-4 min-[400px]:grid-cols-2">
      <Field name="occupiedAreaM2" label="Área deste cultivo (m²)" required type="decimal" value={draft.occupied} change={value => change('occupied', value)} error={command.error} />
      <Field name="plantedQuantity" label="Quantidade de plantas" required type="integer" value={draft.quantity} change={value => change('quantity', value)} error={command.error} />
    </div>
    {options.loading && <p role="status">Carregando variedades e origens…</p>}
    {options.error && <Failure error={options.error} retry={options.retry} />}
    <details className="!m-0 border-t border-[#c8d3b6] pt-2"><summary>Variedade e informações complementares</summary><div className="mt-3 grid gap-4">
      <SelectField name="cultivarId" label="Variedade (opcional)" value={draft.cultivar} change={value => change('cultivar', value)} options={options.data?.cultivar?.filter(item => item.speciesId === species.id) ?? []} error={command.error} />
      <SelectField name="propagationLotId" label="Lote de origem (opcional)" value={draft.origin} change={value => change('origin', value)} options={options.data?.propagationLot?.filter(item => item.speciesId === species.id) ?? []} error={command.error} />
      <Field name="plantSpacingCm" label="Entre plantas (cm)" type="decimal" value={draft.plantSpacing} change={value => change('plantSpacing', value)} error={command.error} />
      <Field name="rowSpacingCm" label="Entre linhas (cm)" type="decimal" value={draft.rowSpacing} change={value => change('rowSpacing', value)} error={command.error} />
      <SelectField name="responsibleParticipantId" label="Responsável (opcional)" value={draft.participant} change={value => change('participant', value)} options={options.data?.participant ?? []} error={command.error} />
    </div></details>
    {command.progress && <p role="status" className="notice !m-0">{command.progress}</p>}
    {command.partialCycle && <button type="button" className="secondary" onClick={() => { releaseNavigationGuard(); resume(command.partialCycle ?? '') }}>Revisar plantio do cultivo salvo</button>}
  </EditorFrame>
}
