import { readRoute } from '../../../routes/navigation'
import { linkTo } from '../../../routes/routes'
import { useGardenState } from "../../../hooks/useGardenState"
import { useCallback, useState } from 'react'
import { Field, SelectField, Facts } from '../../../components/Fields'
import { Failure, Pagination } from '../../../components/Feedback'
import { CommandFeedback } from '../../../components/CommandFeedback'
import { useCommand } from '../../../hooks/useCommand'
import { useRemote } from '../../../hooks/useRemote'
import { useLookups } from '../../../hooks/useLookups'
import { today, formatNumber } from '../../irrigation/formValues'
import { formatDate } from '../../../services/dates'
import { numeric, optionalText, requiredNumber } from '../../../services/values'
import { bedApi } from '../services/BedApi'
import { bedContextApi } from '../services/bedContextApi'
import type { Bed } from '../types/Bed'

const period = (from: string, until: string | null) => until ? `${formatDate(from)} → ${formatDate(until)}` : `Desde ${formatDate(from)}`

/**
 * Área e setor do canteiro ao longo do tempo: situação atual em destaque, histórico de vigências visível e o
 * registro de mudança recolhido (aberto quando há pendência). Cada mudança cria nova vigência e preserva a anterior.
 */
export function BedContext({ garden, bed, changed }: { garden: string; bed: Bed; changed: () => void }) {
  const workspace = useGardenState()
  const [revision, setRevision] = useState(0), [configPage, setConfigPage] = useState(1), [sectorPage, setSectorPage] = useState(1), [on, setOn] = useState(today()), [queryOn, setQueryOn] = useState(today())
  const [area, setArea] = useState(''), [length, setLength] = useState(''), [width, setWidth] = useState(''), [validFrom, setValidFrom] = useState(today()), [sector, setSector] = useState(readRoute().search.get('sectorId') || ''), [reason, setReason] = useState('')
  const refresh = () => { setRevision(x => x + 1); changed() }
  const command = useCommand(bed, () => bedApi.get(garden, bed.id), refresh)
  const options = useLookups(garden, ['irrigationSector'])
  const configurationsLoad = useCallback((signal: AbortSignal) => bedContextApi.configurations(garden, bed.id, configPage, signal), [garden, bed.id, configPage])
  const assignmentsLoad = useCallback((signal: AbortSignal) => bedContextApi.assignments(garden, bed.id, sectorPage, signal), [garden, bed.id, sectorPage])
  const atLoad = useCallback((signal: AbortSignal) => bedContextApi.on(garden, bed.id, queryOn, signal), [garden, bed.id, queryOn])
  const configurations = useRemote(`${bed.id}/${configPage}/${revision}/${bed.version}`, configurationsLoad), assignments = useRemote(`${bed.id}/${sectorPage}/${revision}/${bed.version}`, assignmentsLoad), at = useRemote(`${bed.id}/${queryOn}/${revision}/${bed.version}`, atLoad)
  const sectorName = (id: string) => options.data?.irrigationSector?.find(x => x.id === id)?.label ?? 'Setor'
  const pending = !bed.configuration || !bed.sectorAssignment
  const dimensions = bed.configuration?.lengthMeters && bed.configuration.widthMeters ? `${formatNumber(bed.configuration.lengthMeters)} × ${formatNumber(bed.configuration.widthMeters)} m` : undefined
  const noSectors = !options.loading && !options.error && !options.data?.irrigationSector?.some(item => item.isActive !== false)

  return <section className="bed-context panel" aria-labelledby="bed-context-title">
    <h2 id="bed-context-title" className="sr-only">Área e setor</h2>

    <div className="bed-context-current" aria-label="Situação atual">
      <div data-pending={!bed.configuration || undefined}>
        <span>Área cultivável</span>
        <strong>{bed.configuration ? `${formatNumber(bed.configuration.cultivableAreaM2)} m²` : 'Pendente'}</strong>
        <small>{bed.configuration ? [dimensions, `desde ${formatDate(bed.configuration.validFrom)}`].filter(Boolean).join(' · ') : 'Necessária para abrir ciclos de cultivo'}</small>
      </div>
      <div data-pending={!bed.sectorAssignment || undefined}>
        <span>Setor</span>
        <strong>{bed.sectorAssignment ? sectorName(bed.sectorAssignment.sectorId) : 'Pendente'}</strong>
        <small>{bed.sectorAssignment ? `desde ${formatDate(bed.sectorAssignment.validFrom)}` : 'Necessário para aparecer na irrigação'}</small>
      </div>
    </div>

    {bed.isActive && workspace.isActive && <details className="bed-context-change" open={pending || undefined}>
      <summary>Registrar mudança de área ou setor</summary>
      <p className="hint">A mudança vale a partir da data informada; a vigência anterior é preservada no histórico.</p>
      <CommandFeedback {...command} compare={() => void command.compare()} review={command.review && <Facts items={[{ label: 'Versão atual', value: command.review.version }, { label: 'Área vigente (m²)', value: command.review.configuration?.cultivableAreaM2 }, { label: 'Setor vigente', value: command.review.sectorAssignment ? sectorName(command.review.sectorAssignment.sectorId) : undefined }]} />}/>
      <div className="grid gap-4 sm:grid-cols-[minmax(0,14rem)_minmax(0,1fr)]">
        <Field name="validFrom" label="A partir de" required type="date" value={validFrom} change={setValidFrom} error={command.error} />
        <Field name="reason" label="Motivo" required={Boolean(bed.configuration || bed.sectorAssignment)} type="textarea" maxLength={500} value={reason} change={setReason} error={command.error} />
      </div>
      <div className="bed-context-forms">
        <form onSubmit={event => { event.preventDefault(); void command.submit(version => bedContextApi.configure(garden, bed.id, { cultivableAreaM2: requiredNumber(area, 'cultivableAreaM2'), lengthMeters: numeric(length, 'lengthMeters'), widthMeters: numeric(width, 'widthMeters'), validFrom, reason: optionalText(reason), expectedBedVersion: version })) }}>
          <h3>Área cultivável</h3>
          <fieldset disabled={command.busy} className="grid gap-3 sm:grid-cols-3">
            <Field name="cultivableAreaM2" label="Área (m²)" required type="decimal" value={area} change={setArea} error={command.error} />
            <Field name="lengthMeters" label="Comprimento (m)" type="decimal" value={length} change={setLength} error={command.error} />
            <Field name="widthMeters" label="Largura (m)" type="decimal" value={width} change={setWidth} error={command.error} />
            <div className="sm:col-span-3"><button disabled={command.error?.status === 409}>Salvar área</button></div>
          </fieldset>
        </form>
        <form onSubmit={event => { event.preventDefault(); void command.submit(version => bedContextApi.assign(garden, bed.id, { sectorId: sector, validFrom, reason: optionalText(reason), expectedBedVersion: version })) }}>
          <h3>Setor</h3>
          <fieldset disabled={command.busy} className="grid gap-3">
            <SelectField name="sectorId" label="Novo setor" required value={sector} change={setSector} options={options.data?.irrigationSector ?? []} error={command.error} />
            <div><button disabled={command.error?.status === 409 || options.loading || !!options.error}>Salvar setor</button></div>
          </fieldset>
          {noSectors && <p className="notice">Cadastre um setor antes de fazer a associação. <a className="underline" href={linkTo(garden, 'horta/setores', 'novo')}>Cadastrar setor</a></p>}
          {options.error && <Failure error={options.error} retry={options.retry} />}
        </form>
      </div>
    </details>}

    <div className="bed-context-history">
      <section aria-label="Histórico de área">
        <h3>Histórico de área</h3>
        {configurations.error && <Failure error={configurations.error} retry={refresh} />}
        {configurations.data && (configurations.data.items.length === 0 ? <p className="bed-context-empty">Nenhuma área registrada.</p> : <ul>{configurations.data.items.map(item => <li key={item.id}><span>{period(item.validFrom, item.validUntil)}</span><strong>{formatNumber(item.cultivableAreaM2)} m²</strong></li>)}</ul>)}
        {configurations.data && <Pagination data={configurations.data} change={setConfigPage} label="Páginas de configurações" />}
      </section>
      <section aria-label="Histórico de setor">
        <h3>Histórico de setor</h3>
        {assignments.error && <Failure error={assignments.error} retry={refresh} />}
        {assignments.data && (assignments.data.items.length === 0 ? <p className="bed-context-empty">Nenhum setor associado.</p> : <ul>{assignments.data.items.map(item => <li key={item.id}><span>{period(item.validFrom, item.validUntil)}</span><strong>{sectorName(item.sectorId)}</strong></li>)}</ul>)}
        {assignments.data && <Pagination data={assignments.data} change={setSectorPage} label="Páginas de associações" />}
      </section>
    </div>

    <details className="bed-context-query">
      <summary>Consultar a situação numa data</summary>
      <form className="flex flex-wrap items-end gap-3" onSubmit={event => { event.preventDefault(); setQueryOn(on) }}><Field name="on" label="Data" required type="date" value={on} change={setOn} /><button className="secondary">Consultar</button></form>
      {at.loading && <p role="status">Consultando data…</p>}{at.error && <Failure error={at.error} retry={refresh} />}
      {at.data && <Facts items={[{ label: `Área em ${formatDate(queryOn)}`, value: at.data.configuration ? `${formatNumber(at.data.configuration.cultivableAreaM2)} m²` : 'Sem configuração na data' }, { label: `Setor em ${formatDate(queryOn)}`, value: at.data.sectorAssignment ? sectorName(at.data.sectorAssignment.sectorId) : 'Sem associação na data' }]} />}
    </details>
  </section>
}
