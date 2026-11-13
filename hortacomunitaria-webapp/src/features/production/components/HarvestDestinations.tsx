import { linkTo } from '../../../routes/routes'
import { useCallback, useState } from 'react'
import { useRemote } from '../../../hooks/useRemote'
import { Failure } from '../../../components/Feedback'
import { formatDate } from '../../../services/dates'
import { formatNumber } from '../../irrigation/formValues'
import { harvestDestinationsApi } from '../services/harvestDestinationsApi'

/** Lotes de secagem que recebem massa desta colheita, no formato dos grupos de registros do ciclo. */
export function HarvestDestinations({ garden, harvest }: { garden: string; harvest: string }) {
  const [revision, setRevision] = useState(0)
  const load = useCallback((signal: AbortSignal) => harvestDestinationsApi.list(garden, harvest, signal), [garden, harvest])
  const result = useRemote(`${garden}/${harvest}/${revision}`, load)
  const lines = result.data?.flatMap(lot => lot.items.filter(item => item.harvestId === harvest).map((item, index) => ({ key: `${lot.id}/${index}`, lot, mass: item.allocatedMassKg }))) ?? []
  return <>
    {result.loading && <p role="status">Carregando destinações…</p>}
    {result.error && <Failure error={result.error} retry={() => setRevision(x => x + 1)} />}
    {result.data && <section className="cycle-record-group" aria-label="Destinações para secagem">
      <header><h3>Destinações para secagem <span>({lines.length})</span></h3></header>
      {lines.length === 0 ? <p className="cycle-record-empty">Nenhuma destinação válida para esta colheita.</p> : <ul>
        {lines.map(line => <li key={line.key}><a href={linkTo(garden, 'producao/secagens', line.lot.id)}>
          <strong>{line.lot.code || `Secagem de ${formatDate(line.lot.startedOn)}`}</strong>
          <span>{formatNumber(line.mass)} kg destinados · início em {formatDate(line.lot.startedOn)} · {line.lot.status === 'Completed' ? 'Concluído' : 'Em andamento'}</span>
        </a></li>)}
      </ul>}
    </section>}
  </>
}
