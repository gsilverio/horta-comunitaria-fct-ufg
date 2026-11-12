import type { AppRoute } from '../../../routes/routes'
import { linkTo } from '../../../routes/routes'
import { useGardenState } from "../../../hooks/useGardenState"
import { useCallback, useState } from 'react'
import { Field, Facts } from '../../../components/Fields'
import { CommandFeedback } from '../../../components/CommandFeedback'
import { useCommand } from '../../../hooks/useCommand'
import { useRemote } from '../../../hooks/useRemote'
import { today, FormFailure } from '../../irrigation/formValues'
import { cultivationCycleApi } from '../services/CultivationCycleApi'
import { plantingApi } from '../services/PlantingApi'
import { agronomicObservationApi } from '../services/AgronomicObservationApi'
import { harvestApi } from '../../production/services/HarvestApi'
import { Failure, Pagination } from '../../../components/Feedback'
import type { CultivationCycle } from '../types/CultivationCycle'

export function CycleActions({ route, record, changed }: { route: AppRoute; record: CultivationCycle; changed: () => void }) {
  const workspace = useGardenState()
  const [endedOn, setEndedOn] = useState(today()), [reason, setReason] = useState(''), [pages, setPages] = useState([1, 1, 1]), [revision, setRevision] = useState(0)
  const command = useCommand(record, () => cultivationCycleApi.get(route.garden, record.id), changed)
  const load = useCallback((signal: AbortSignal) => Promise.all([plantingApi.list(route.garden, { from: '0001-01-01', to: '9998-12-31', cycleId: record.id, page: pages[0], pageSize: 5 }, signal), agronomicObservationApi.list(route.garden, { from: '0001-01-01', to: '9998-12-31', cycleId: record.id, page: pages[1], pageSize: 5 }, signal), harvestApi.list(route.garden, { from: '0001-01-01', to: '9998-12-31', cycleId: record.id, page: pages[2], pageSize: 5 }, signal)]), [route.garden, record.id, pages])
  const related = useRemote(`${record.id}/${record.version}/${pages.join("/")}/${revision}`, load), query = new URLSearchParams({ cycleId: record.id, from: '0001-01-01', to: '9998-12-31' })
  return <section className="mt-4 space-y-3"><h3>Registros do ciclo</h3>{record.validity === 'Valid' && workspace.isActive && <div className="next-step"><a className="action-link" href={linkTo(route.garden, 'cultivo/plantios', 'novo', new URLSearchParams({ cycleId: record.id }))}>Registrar plantio neste ciclo</a><a href={linkTo(route.garden, 'cultivo/observacoes', 'novo', new URLSearchParams({ cycleId: record.id }))}>Registrar observação neste ciclo</a><a href={linkTo(route.garden, 'producao/colheitas', 'novo', new URLSearchParams({ cycleId: record.id }))}>Registrar colheita neste ciclo</a></div>}<div className="flex flex-wrap gap-4"><a href={linkTo(route.garden, 'cultivo/plantios', '', query)}>Plantios e avaliações</a><a href={linkTo(route.garden, 'cultivo/observacoes', '', query)}>Observações</a><a href={linkTo(route.garden, 'producao/colheitas', '', query)}>Colheitas</a></div>{related.loading && <p role="status">Carregando registros do ciclo…</p>}{related.error && <Failure error={related.error} retry={() => setRevision(x => x + 1)} />}{related.data && related.data.map((group, index) => <div key={index}><h4>{['Plantios', 'Observações', 'Colheitas'][index]}</h4><ul>{group.items.map(item => <li className="py-2" key={item.id}><a href={linkTo(route.garden, (['cultivo/plantios', 'cultivo/observacoes', 'producao/colheitas'] as const)[index], item.id, query)}>Abrir registro · {item.id.slice(-6)}</a></li>)}</ul><Pagination data={group} change={page => setPages(current => current.map((value, i) => i === index ? page : value))} label={`Páginas de ${['plantios', 'observações', 'colheitas'][index]} do ciclo`} /></div>)}
    {record.status === 'InProgress' && record.validity === 'Valid' && workspace.isActive && <details className="organization-disclosure"><summary>Encerrar ciclo</summary><CommandFeedback {...command} compare={() => void command.compare()} review={command.review && <Facts items={[{ label: 'Versão', value: command.review.version }, { label: 'Início', value: command.review.startedOn }, { label: 'Fim', value: command.review.endedOn }, { label: 'Situação', value: command.review.status }]} />} /><form className="grid gap-4 sm:grid-cols-2" onSubmit={event => { event.preventDefault(); void command.submit(version => { if (!reason.trim()) throw new FormFailure({ reason: ['Informe o motivo.'] }); return cultivationCycleApi.close(route.garden, record.id, endedOn, version, reason.trim()) }) }}><fieldset disabled={command.busy}><Field name="endedOn" label="Data de encerramento" required type="date" value={endedOn} change={setEndedOn} error={command.error} /><Field name="reason" label="Motivo do encerramento" required type="textarea" maxLength={500} value={reason} change={setReason} error={command.error} /><button disabled={command.error?.status === 409}>Confirmar encerramento</button></fieldset></form></details>}
  </section>
}
