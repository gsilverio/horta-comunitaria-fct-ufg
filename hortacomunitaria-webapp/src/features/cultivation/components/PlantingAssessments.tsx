import { useCallback, useState } from 'react'
import { useGardenState } from '../../../hooks/useGardenState'
import { useRemote } from '../../../hooks/useRemote'
import { routeHref } from '../../../hooks/useRoute'
import { Failure, Pagination } from '../../../components/Feedback'
import { establishmentAssessmentApi } from '../services/EstablishmentAssessmentApi'
import type { Planting } from '../types/Planting'
export function PlantingAssessments({ garden, record }: { garden: string; record: Planting }) {
  const [page, setPage] = useState(1), [revision, setRevision] = useState(0), workspace = useGardenState()
  const load = useCallback((signal: AbortSignal) => establishmentAssessmentApi.list(garden, { from: '0001-01-01', to: '9998-12-31', plantingId: record.id, page, pageSize: 5 }, signal), [garden, record.id, page])
  const result = useRemote(`${garden}/${record.id}/${record.version}/${page}/${revision}`, load), query = new URLSearchParams({ plantingId: record.id, from: record.plantedOn, to: '9998-12-31' })
  return <section className="mt-6"><h3>Avaliações deste plantio</h3>{record.validity === 'Valid' && workspace.isActive && <a className="inline-block py-3 underline" href={routeHref(garden, 'cultivo', 'avaliacoes', 'novo', query)}>Registrar avaliação deste plantio</a>}{result.loading && <p role="status">Carregando avaliações…</p>}{result.error && <Failure error={result.error} retry={() => setRevision(x => x + 1)} />}{result.data && <><ul>{result.data.items.map(item => <li key={item.id} className="py-3"><a className="underline" href={routeHref(garden, 'cultivo', 'avaliacoes', item.id, query)}>{item.assessedOn.split('-').reverse().join('/')} · {item.survivingQuantity} sobreviventes · {item.method} · {item.validity === 'Invalid' ? 'Invalidado' : 'Válido'}</a></li>)}</ul>{result.data.totalCount === 0 && <p>Nenhuma avaliação neste plantio.</p>}<Pagination data={result.data} change={setPage} label="Páginas de avaliações do plantio" /></>}</section>
}
