import { readRoute, routeHref } from '../../../hooks/useRoute'
import { useGardenState } from "../../../hooks/useGardenState"
import { useCallback, useState } from 'react'
import { Field, SelectField, Facts } from '../../../components/Fields'
import { Failure, Pagination } from '../../../components/Feedback'
import { CommandFeedback } from '../../../components/CommandFeedback'
import { useCommand } from '../../../hooks/useCommand'
import { useRemote } from '../../../hooks/useRemote'
import { useLookups } from '../../../hooks/useLookups'
import { today, formatNumber } from '../../irrigation/formValues'
import { numeric, optionalText, requiredNumber } from '../../../services/values'
import { bedApi } from '../services/BedApi'
import { bedContextApi } from '../services/bedContextApi'
import type { Bed } from '../types/Bed'

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
  return <section className="bed-setup panel">
    <h2 className="mb-0.5">Área e setor ao longo do tempo</h2><p className="text-horta-muted">Consulte configurações anteriores ou registre uma mudança a partir de outra data.</p>
    {bed.isActive && workspace.isActive && <section aria-label="Configuração do canteiro">
      <h4 className="font-semibold">Adicionar configuração ou associação</h4>
      <CommandFeedback {...command} compare={() => void command.compare()} review={command.review && <Facts items={[{ label: 'Versão atual', value: command.review.version }, { label: 'Área vigente (m²)', value: command.review.configuration?.cultivableAreaM2 }, { label: 'Setor vigente', value: command.review.sectorAssignment?.sectorId }]} />}/>
      <div className="my-3 grid gap-4 sm:grid-cols-2"><Field name="validFrom" label="Início da nova vigência" required type="date" value={validFrom} change={setValidFrom} error={command.error} /><Field name="reason" label="Motivo da vigência" type="textarea" maxLength={500} value={reason} change={setReason} error={command.error} /></div>
      <p className="hint">Use uma data igual ou anterior aos registros que pretende lançar. Em alterações posteriores, informe o motivo; o histórico anterior será preservado.</p>
      <div className="grid gap-6 xl:grid-cols-2">
      <form className="setup-form" onSubmit={event => { event.preventDefault(); void command.submit(version => bedContextApi.configure(garden, bed.id, { cultivableAreaM2: requiredNumber(area, 'cultivableAreaM2'), lengthMeters: numeric(length, 'lengthMeters'), widthMeters: numeric(width, 'widthMeters'), validFrom, reason: optionalText(reason), expectedBedVersion: version })) }}>
        <h3>1. Área cultivável</h3><p className="hint">{bed.configuration ? `Área atual: ${formatNumber(bed.configuration.cultivableAreaM2)} m². Uma nova configuração cria outra vigência.` : 'Pendente: informe a área para permitir ciclos de cultivo.'}</p>
        <fieldset disabled={command.busy} className="grid gap-4"><Field name="cultivableAreaM2" label="Área cultivável (m²)" required type="decimal" value={area} change={setArea} error={command.error} /><Field name="lengthMeters" label="Comprimento (m)" type="decimal" value={length} change={setLength} error={command.error} /><Field name="widthMeters" label="Largura (m)" type="decimal" value={width} change={setWidth} error={command.error} /><button className="secondary" disabled={command.error?.status === 409}>Salvar configuração de área</button></fieldset>
      </form>
      <form className="setup-form" onSubmit={event => { event.preventDefault(); void command.submit(version => bedContextApi.assign(garden, bed.id, { sectorId: sector, validFrom, reason: optionalText(reason), expectedBedVersion: version })) }}>
        <h3>2. Associação ao setor</h3><p className="hint">{bed.sectorAssignment ? 'Já existe uma associação vigente. Mudanças de setor preservam os registros anteriores.' : 'Pendente: associe o canteiro para que apareça na irrigação.'}</p>
        <fieldset disabled={command.busy} className="grid gap-4"><SelectField name="sectorId" label="Setor da nova associação" required value={sector} change={setSector} options={options.data?.irrigationSector ?? []} error={command.error} /><button className="secondary" disabled={command.error?.status === 409 || options.loading || !!options.error}>Salvar associação de setor</button></fieldset>
        {!options.loading && !options.error && !options.data?.irrigationSector?.some(item => item.isActive !== false) && <p className="notice">Cadastre um setor antes de fazer a associação. <a className="underline" href={routeHref(garden, 'horta', 'setores', 'novo')}>Cadastrar setor</a></p>}
        {options.error && <Failure error={options.error} retry={options.retry} />}
      </form></div>
      
    </section>}
    <details className="mt-6"><summary>Consultar área, setor e histórico de vigências</summary>
      <p>Início inclusivo; fim exclusivo. A consulta mostra a organização válida na data escolhida.</p>
      <form className="flex flex-wrap items-end gap-3" onSubmit={event => { event.preventDefault(); setQueryOn(on) }}><Field name="on" label="Consultar na data" required type="date" value={on} change={setOn} /><button className="secondary">Consultar vigência</button></form>
      {at.loading && <p role="status">Consultando data…</p>}{at.error && <Failure error={at.error} retry={refresh} />}{at.data && <Facts items={[{ label: `Área em ${queryOn}`, value: at.data.configuration ? `${formatNumber(at.data.configuration.cultivableAreaM2)} m²` : 'Sem configuração na data' }, { label: `Setor em ${queryOn}`, value: at.data.sectorAssignment ? options.data?.irrigationSector?.find(x => x.id === at.data?.sectorAssignment?.sectorId)?.label ?? at.data.sectorAssignment.sectorId : 'Sem associação na data' }]} />}
      <h4>Configurações de área</h4>{configurations.error && <Failure error={configurations.error} retry={refresh} />}{configurations.data && <><ul className="m-0 list-none p-0 text-sm">{configurations.data.items.map(item => <li className="py-2" key={item.id}>{item.validFrom} → {item.validUntil ?? 'sem fim'} · {formatNumber(item.cultivableAreaM2)} m²</li>)}</ul><Pagination data={configurations.data} change={setConfigPage} label="Páginas de configurações" /></>}
      <h4>Associações de setor</h4>{assignments.error && <Failure error={assignments.error} retry={refresh} />}{assignments.data && <><ul className="m-0 list-none p-0 text-sm">{assignments.data.items.map(item => <li className="py-2" key={item.id}>{item.validFrom} → {item.validUntil ?? 'sem fim'} · {options.data?.irrigationSector?.find(x => x.id === item.sectorId)?.label ?? item.sectorId}</li>)}</ul><Pagination data={assignments.data} change={setSectorPage} label="Páginas de associações" /></>}
    </details>
  </section>
}
