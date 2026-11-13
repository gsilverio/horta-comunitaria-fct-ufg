import { linkTo } from '../../../routes/routes'
import { useCallback, useState } from 'react'
import { useGardenState } from '../../../hooks/useGardenState'
import { useRemote } from '../../../hooks/useRemote'
import { Failure } from '../../../components/Feedback'
import { PageNavigator } from '../../../components/PageNavigator'
import { formatDate } from '../../../services/dates'
import { formatNumber } from '../../irrigation/formValues'
import { establishmentAssessmentApi } from '../services/EstablishmentAssessmentApi'
import type { Planting } from '../types/Planting'

/** Avaliações de estabelecimento do plantio, no mesmo formato dos grupos de registros do ciclo. */
export function PlantingAssessments({ garden, record }: { garden: string; record: Planting }) {
  const [page, setPage] = useState(1), [revision, setRevision] = useState(0), workspace = useGardenState()
  const load = useCallback((signal: AbortSignal) => establishmentAssessmentApi.list(garden, { from: '0001-01-01', to: '9998-12-31', plantingId: record.id, page, pageSize: 5 }, signal), [garden, record.id, page])
  const result = useRemote(`${garden}/${record.id}/${record.version}/${page}/${revision}`, load), query = new URLSearchParams({ plantingId: record.id, from: record.plantedOn, to: '9998-12-31' })
  const canRegister = record.validity === 'Valid' && workspace.isActive

  return <div className="cycle-records">
    {canRegister && <div className="cycle-record-actions" role="group" aria-label="Registrar neste plantio">
      <a className="action-link" href={linkTo(garden, 'cultivo/avaliacoes', 'novo', query)}>Registrar avaliação</a>
    </div>}
    {result.loading && <p role="status">Carregando avaliações…</p>}
    {result.error && <Failure error={result.error} retry={() => setRevision(x => x + 1)} />}
    {result.data && <section className="cycle-record-group" aria-label="Avaliações deste plantio">
      <header><h3>Avaliações <span>({result.data.totalCount})</span></h3>{result.data.totalCount > 0 && <a href={linkTo(garden, 'cultivo/avaliacoes', '', query)}>Ver todas</a>}</header>
      {result.data.items.length === 0 ? <p className="cycle-record-empty">Nenhuma avaliação neste plantio.</p> : <ul>
        {result.data.items.map(item => <li key={item.id}><a href={linkTo(garden, 'cultivo/avaliacoes', item.id, query)}>
          <strong>{formatDate(item.assessedOn)}</strong>{item.validity === 'Invalid' && <span className="badge invalid">Invalidado</span>}
          <span>{formatNumber(item.survivingQuantity)} sobreviventes · {item.method}</span>
        </a></li>)}
      </ul>}
      {result.data.totalPages > 1 && <PageNavigator className="org-pagination" data={result.data} change={setPage} label="Páginas de avaliações do plantio" />}
    </section>}
  </div>
}
