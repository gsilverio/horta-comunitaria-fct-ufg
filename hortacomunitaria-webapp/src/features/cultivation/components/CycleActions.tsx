import type { ReactNode } from 'react'
import type { AppRoute, PageKey } from '../../../routes/routes'
import { linkTo } from '../../../routes/routes'
import { useGardenState } from "../../../hooks/useGardenState"
import { useCallback, useState } from 'react'
import { Field, Facts } from '../../../components/Fields'
import { CommandFeedback } from '../../../components/CommandFeedback'
import { useCommand } from '../../../hooks/useCommand'
import { useRemote } from '../../../hooks/useRemote'
import { today, FormFailure, formatNumber } from '../../irrigation/formValues'
import { formatDate } from '../../../services/dates'
import { enumLabels } from '../../../services/labels'
import { cultivationCycleApi } from '../services/CultivationCycleApi'
import { plantingApi } from '../services/PlantingApi'
import { agronomicObservationApi } from '../services/AgronomicObservationApi'
import { harvestApi } from '../../production/services/HarvestApi'
import { Failure, Pagination } from '../../../components/Feedback'
import type { Page } from '../../../types/api'
import type { CultivationCycle } from '../types/CultivationCycle'

interface RecordLine { id: string; invalid: boolean; title: string; detail?: string }

/** Grupo de registros do ciclo: título com contagem, linhas com resumo e estado vazio explícito. */
function RecordGroup({ title, empty, page, all, href, change, lines }: {
  title: string; empty: string; page: Page<unknown>; all: string; href: (id: string) => string; change: (page: number) => void; lines: RecordLine[]
}) {
  return <section className="cycle-record-group" aria-label={title}>
    <header><h3>{title} <span>({page.totalCount})</span></h3>{page.totalCount > 0 && <a href={all}>Ver todos</a>}</header>
    {lines.length === 0 ? <p className="cycle-record-empty">{empty}</p> : <ul>
      {lines.map(line => <li key={line.id}><a href={href(line.id)}>
        <strong>{line.title}</strong>{line.invalid && <span className="badge invalid">Invalidado</span>}
        {line.detail && <span>{line.detail}</span>}
      </a></li>)}
    </ul>}
    <Pagination data={page} change={change} label={`Páginas de ${title.toLocaleLowerCase('pt-BR')} do ciclo`} />
  </section>
}

/** Registros vinculados ao ciclo (plantios, observações, colheitas), atalhos de registro e encerramento. */
export function CycleActions({ route, record, changed }: { route: AppRoute; record: CultivationCycle; changed: () => void }) {
  const workspace = useGardenState()
  const [endedOn, setEndedOn] = useState(today()), [reason, setReason] = useState(''), [pages, setPages] = useState([1, 1, 1]), [revision, setRevision] = useState(0)
  const command = useCommand(record, () => cultivationCycleApi.get(route.garden, record.id), changed)
  const load = useCallback((signal: AbortSignal) => Promise.all([plantingApi.list(route.garden, { from: '0001-01-01', to: '9998-12-31', cycleId: record.id, page: pages[0], pageSize: 5 }, signal), agronomicObservationApi.list(route.garden, { from: '0001-01-01', to: '9998-12-31', cycleId: record.id, page: pages[1], pageSize: 5 }, signal), harvestApi.list(route.garden, { from: '0001-01-01', to: '9998-12-31', cycleId: record.id, page: pages[2], pageSize: 5 }, signal)]), [route.garden, record.id, pages])
  const related = useRemote(`${record.id}/${record.version}/${pages.join("/")}/${revision}`, load), query = new URLSearchParams({ cycleId: record.id, from: '0001-01-01', to: '9998-12-31' })
  const canRegister = record.validity === 'Valid' && workspace.isActive
  const changePage = (index: number) => (page: number) => setPages(current => current.map((value, i) => i === index ? page : value))
  const open = (key: PageKey) => (id: string) => linkTo(route.garden, key, id, query)
  const create = (key: PageKey, label: string): ReactNode => <a className="action-link" href={linkTo(route.garden, key, 'novo', new URLSearchParams({ cycleId: record.id }))}>{label}</a>

  return <div className="cycle-records">
    {canRegister && <div className="cycle-record-actions" role="group" aria-label="Registrar neste ciclo">
      {create('cultivo/plantios', 'Registrar plantio')}
      {create('cultivo/observacoes', 'Registrar observação')}
      {create('producao/colheitas', 'Registrar colheita')}
    </div>}
    {related.loading && <p role="status">Carregando registros do ciclo…</p>}
    {related.error && <Failure error={related.error} retry={() => setRevision(x => x + 1)} />}
    {related.data && <div className="cycle-record-groups">
      <RecordGroup title="Plantios" empty="Nenhum plantio registrado." page={related.data[0]} all={linkTo(route.garden, 'cultivo/plantios', '', query)} href={open('cultivo/plantios')} change={changePage(0)}
        lines={related.data[0].items.map(item => ({ id: item.id, invalid: item.validity === 'Invalid', title: formatDate(item.plantedOn), detail: `${enumLabels[item.kind] ?? item.kind} · ${formatNumber(item.plantedQuantity)} plantas` }))} />
      <RecordGroup title="Observações" empty="Nenhuma observação registrada." page={related.data[1]} all={linkTo(route.garden, 'cultivo/observacoes', '', query)} href={open('cultivo/observacoes')} change={changePage(1)}
        lines={related.data[1].items.map(item => ({ id: item.id, invalid: item.validity === 'Invalid', title: `${formatDate(item.observedOn)} · ${item.category}`, detail: item.description }))} />
      <RecordGroup title="Colheitas" empty="Nenhuma colheita registrada." page={related.data[2]} all={linkTo(route.garden, 'producao/colheitas', '', query)} href={open('producao/colheitas')} change={changePage(2)}
        lines={related.data[2].items.map(item => ({ id: item.id, invalid: item.validity === 'Invalid', title: formatDate(item.harvestedOn), detail: `${formatNumber(item.freshMassKg)} kg colhidos${item.usableMassKg !== null ? ` · ${formatNumber(item.usableMassKg)} kg aproveitáveis` : ''}` }))} />
    </div>}
    {record.status === 'InProgress' && canRegister && <details className="cycle-close">
      <summary>Encerrar ciclo</summary>
      <p className="hint">Encerre quando não houver mais plantas deste cultivo no canteiro. O ciclo continua consultável e pode ser corrigido com motivo.</p>
      <CommandFeedback {...command} compare={() => void command.compare()} review={command.review && <Facts items={[{ label: 'Versão', value: command.review.version }, { label: 'Início', value: command.review.startedOn }, { label: 'Fim', value: command.review.endedOn }, { label: 'Situação', value: command.review.status }]} />} />
      <form onSubmit={event => { event.preventDefault(); void command.submit(version => { if (!reason.trim()) throw new FormFailure({ reason: ['Informe o motivo.'] }); return cultivationCycleApi.close(route.garden, record.id, endedOn, version, reason.trim()) }) }}>
        <fieldset disabled={command.busy} className="grid gap-4 sm:grid-cols-[minmax(0,14rem)_minmax(0,1fr)]">
          <Field name="endedOn" label="Data de encerramento" required type="date" value={endedOn} change={setEndedOn} error={command.error} />
          <Field name="reason" label="Motivo do encerramento" required type="textarea" maxLength={500} value={reason} change={setReason} error={command.error} />
          <div className="sm:col-span-2"><button className="danger-outline" disabled={command.error?.status === 409}>Confirmar encerramento</button></div>
        </fieldset>
      </form>
    </details>}
  </div>
}
