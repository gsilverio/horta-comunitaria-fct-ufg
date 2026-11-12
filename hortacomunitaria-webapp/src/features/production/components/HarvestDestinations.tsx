import { linkTo } from '../../../routes/routes'
import { useCallback, useState } from 'react'
import { useRemote } from '../../../hooks/useRemote'
import { Failure } from '../../../components/Feedback'
import { formatNumber } from '../../irrigation/formValues'
import { harvestDestinationsApi } from '../services/harvestDestinationsApi'
export function HarvestDestinations({ garden, harvest }: { garden: string; harvest: string }) {
  const [revision, setRevision] = useState(0)
  const load = useCallback((signal: AbortSignal) => harvestDestinationsApi.list(garden, harvest, signal), [garden, harvest])
  const result = useRemote(`${garden}/${harvest}/${revision}`, load)
  return <section className="mt-6"><h3>Destinações válidas para secagem</h3>{result.loading && <p role="status">Carregando destinações…</p>}{result.error && <Failure error={result.error} retry={() => setRevision(x => x + 1)} />}{result.data && <><ul>{result.data.flatMap(lot => lot.items.filter(item => item.harvestId === harvest).map(item => <li className="py-3" key={lot.id}><a className="underline" href={linkTo(garden, 'producao/secagens', lot.id)}>Secagem de {lot.startedOn.split("-").reverse().join("/")} · {formatNumber(item.allocatedMassKg)} kg destinados · {lot.status === 'Completed' ? 'Concluído' : 'Em andamento'}</a></li>))}</ul>{result.data.length === 0 && <p>Nenhuma destinação válida para esta colheita.</p>}</>}</section>
}
