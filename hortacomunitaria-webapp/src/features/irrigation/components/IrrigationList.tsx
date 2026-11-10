import { Pagination } from '../../../components/Feedback'
import { formatInstant, formatVolume, statusLabel } from '../formValues'
import type { Page } from '../../../types/api'
import type { IrrigationSummary } from '../types/irrigation'

export function IrrigationList({ data, open, changePage }: { data: Page<IrrigationSummary>; open: (id: string) => void; changePage: (page: number) => void }) {
  return <section className="panel flush" aria-labelledby="records-title"><div className="panel-header"><div><h2 id="records-title" className="m-0 text-sm">Registros de irrigação</h2></div><span className="count">{data.totalCount} registros</span></div>
    {data.items.length === 0 ? <div className="empty"><span aria-hidden="true">↳</span><h3>Nenhum registro neste período</h3><p>Selecione outro recorte ou registre uma irrigação.</p></div> : <div className="record-list">{data.items.map(item => <article key={item.id} className="record"><div><strong>{item.sectorName}</strong><p>{formatInstant(item.startedAt)} · São Paulo</p><span className={`badge ${item.validity === 'Invalid' ? 'invalid' : ''}`}>{item.validity === 'Invalid' ? 'Invalidado' : statusLabel(item.status)}</span></div><div className="record-end"><strong>{formatVolume(item.volumeLiters)}</strong><button className="secondary" onClick={() => open(item.id)} aria-label={`Abrir registro de ${formatInstant(item.startedAt)}`}>Ver registro →</button></div></article>)}</div>}
    <Pagination data={data} change={changePage} label="Paginação dos registros" /></section>
}

