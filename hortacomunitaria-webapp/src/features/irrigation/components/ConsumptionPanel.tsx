import { Pagination } from '../../../components/Feedback'
import { StatGrid } from '../../../components/StatGrid'
import { formatNumber, formatVolume } from '../formValues'
import type { Consumption, Coverage } from '../types/irrigation'

const durationAvailability = { NoData: 'Sem registros de duração', Unavailable: 'Durações indisponíveis', Partial: 'Durações parcialmente informadas', Available: 'Durações informadas' }
const availability: Record<Coverage['availability'], string> = { NoData: 'Sem dados', Unavailable: 'Volume indisponível', Partial: 'Informação parcial', Available: 'Volumes informados' }
const minutes = (value: number | null) => value === null ? 'Não informada' : `${formatNumber(value)} min`
const hasValue = (value: number | null): value is number => value !== null

export function ConsumptionPanel({ data, changePage }: { data: Consumption; changePage: (page: number) => void }) {
  const summary = data.summary, execution = data.executionSummary
  return <section className="panel flush consumption-panel" aria-labelledby="consumption-title">
    <div className="panel-header">
      <div><h2 id="consumption-title">Consumo conhecido de água</h2><p>Volume contado uma vez por irrigação · datas em {data.timeZone === 'America/Sao_Paulo' ? 'horário de São Paulo' : data.timeZone}</p></div>
      <span className={summary.availability === 'Available' ? 'badge' : 'badge warning'}>{availability[summary.availability]}</span>
    </div>
    <div className="panel-body">
      <StatGrid label="Resumo do consumo" items={[
        { label: 'Volume conhecido', value: <span aria-label="Volume conhecido">{summary.knownVolumeLiters === null ? '—' : formatNumber(summary.knownVolumeLiters)}</span>, unit: 'L', hint: summary.includesEstimatedVolume ? 'Inclui volumes estimados.' : undefined },
        { label: 'Registros com volume', value: `${summary.eventsWithKnownVolume} de ${summary.eventCount}`, hint: `${summary.eventsWithoutVolume} sem volume` },
        { label: 'Concluídas', value: String(execution.completedCount), hint: `${execution.interruptedCount} interrompidas` },
        { label: 'Duração ativa conhecida', value: hasValue(execution.knownActiveDurationMinutes) ? formatNumber(execution.knownActiveDurationMinutes) : '—', unit: hasValue(execution.knownActiveDurationMinutes) ? 'min' : undefined, hint: `${durationAvailability[execution.durationAvailability]} · ${execution.eventsWithoutDuration} sem duração` },
      ]} />
      {summary.availability === 'Unavailable' && <p className="notice mt-3 mb-0">Há irrigações, mas seus volumes não foram coletados.</p>}
      {summary.availability === 'Partial' && <p className="notice mt-3 mb-0">O total conhecido não inclui os registros sem volume.</p>}
    </div>
    {/* Sem dados no período, a tabela por setor só repetiria "Sem dados" em cada linha. */}
    {data.bySector.items.length > 0 && summary.availability !== 'NoData' && <div className="table-scroll"><table className="data-table">
      <caption className="sr-only">Consumo por setor</caption>
      <thead><tr><th scope="col">Setor</th><th scope="col">Cobertura</th><th scope="col" className="num">Volume</th><th scope="col" className="num">Concluídas</th><th scope="col" className="num">Interrompidas</th><th scope="col" className="num">Duração conhecida</th></tr></thead>
      <tbody>{data.bySector.items.map(sector => <tr key={sector.sectorId}>
        <th scope="row" data-label="Setor">{sector.name}</th>
        <td data-label="Cobertura"><span><span className={sector.availability === 'Available' ? 'badge' : 'badge warning'}>{availability[sector.availability]}</span>{sector.includesEstimatedVolume && <span className="ml-2 text-xs text-horta-muted">inclui estimativas</span>}</span></td>
        <td data-label="Volume" className="num font-semibold">{sector.knownVolumeLiters === null ? '—' : formatVolume(sector.knownVolumeLiters)}</td>
        <td data-label="Concluídas" className="num">{sector.completedCount}</td>
        <td data-label="Interrompidas" className="num">{sector.interruptedCount}</td>
        <td data-label="Duração conhecida" className="num"><span>{minutes(sector.knownActiveDurationMinutes)}{sector.eventsWithoutDuration > 0 && <span className="block text-xs text-horta-muted">{sector.eventsWithoutDuration} sem duração</span>}</span></td>
      </tr>)}</tbody>
    </table></div>}
    {summary.availability !== 'NoData' && <Pagination data={data.bySector} change={changePage} label="Paginação dos setores no consumo" />}
  </section>
}
