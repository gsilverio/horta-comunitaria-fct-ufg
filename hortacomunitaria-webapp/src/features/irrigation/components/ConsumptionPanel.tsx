import { Pagination } from '../../../components/Feedback'
import { formatNumber, formatVolume } from '../formValues'
import type { Consumption, Coverage } from '../types/irrigation'

const durationAvailability = { NoData: 'Sem registros de duração', Unavailable: 'Durações indisponíveis', Partial: 'Durações parcialmente informadas', Available: 'Durações informadas' }
const availability: Record<Coverage['availability'], string> = { NoData: 'Sem dados', Unavailable: 'Volume indisponível', Partial: 'Informação parcial', Available: 'Volumes informados' }
export function ConsumptionPanel({ data, changePage }: { data: Consumption; changePage: (page: number) => void }) {
  return <section className="panel consumption" aria-labelledby="consumption-title">
    <div className="flex flex-wrap items-start justify-between gap-3"><div><span className="eyebrow">Indicador do período</span><h2 id="consumption-title">Consumo conhecido de água</h2></div><span className="badge">{availability[data.summary.availability]}</span></div>
    <div className="consumption-value" aria-label="Volume conhecido">{data.summary.knownVolumeLiters === null ? '—' : formatNumber(data.summary.knownVolumeLiters)}<span>L</span></div>
    <p className="muted">{data.summary.eventsWithKnownVolume} de {data.summary.eventCount} registros com volume informado · {data.summary.eventsWithoutVolume} sem volume</p>
    {data.summary.includesEstimatedVolume && <p className="notice">Inclui volumes estimados.</p>}
    {data.summary.availability === 'NoData' && <p>Nenhuma irrigação válida neste recorte.</p>}
    {data.summary.availability === 'Unavailable' && <p>Há irrigações, mas seus volumes não foram coletados.</p>}
    {data.summary.availability === 'Partial' && <p>O total conhecido não inclui os registros sem volume.</p>}
    <p className="hint">O volume é contado uma vez por irrigação. Período inclusivo em {data.timeZone}.</p>
    <p>{data.executionSummary.completedCount} concluídas · {data.executionSummary.interruptedCount} interrompidas</p><p>{durationAvailability[data.executionSummary.durationAvailability]}</p><p>Duração ativa conhecida: {data.executionSummary.knownActiveDurationMinutes === null ? 'Não informada' : `${formatNumber(data.executionSummary.knownActiveDurationMinutes)} min`} · {data.executionSummary.eventsWithoutDuration} sem duração</p>
    <div className="sector-list">{data.bySector.items.map(sector => <div key={sector.sectorId} className="sector-row"><div><strong>{sector.name}</strong><small>{availability[sector.availability]}{sector.includesEstimatedVolume ? ' · inclui estimativas' : ''}</small><small>{sector.completedCount} concluídas · {sector.interruptedCount} interrompidas</small><small>Duração conhecida: {sector.knownActiveDurationMinutes === null ? 'Não informada' : `${formatNumber(sector.knownActiveDurationMinutes)} min`} · {sector.eventsWithoutDuration} sem duração</small></div><strong>{sector.knownVolumeLiters === null ? '—' : formatVolume(sector.knownVolumeLiters)}</strong></div>)}</div>
    <Pagination data={data.bySector} change={changePage} label="Paginação dos setores no consumo" />
  </section>
}

