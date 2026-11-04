import { useCallback, useState } from 'react'
import { Failure, Pagination } from '../../../components/Feedback'
import { useRemote } from '../../../hooks/useRemote'
import { useGardenState } from '../../../hooks/useGardenState'
import { routeHref } from '../../../hooks/useRoute'
import { formatDate } from '../../../services/dates'
import { formatNumber } from '../../irrigation/formValues'
import type { Bed } from '../../organization/types/Bed'
import { bedCultivationApi } from '../services/bedCultivationApi'

export function BedCultivation({ garden, bed }: { garden: string; bed: Bed }) {
  const workspace = useGardenState()
  const [page, setPage] = useState(1), [revision, setRevision] = useState(0)
  const load = useCallback((signal: AbortSignal) => bedCultivationApi.list(garden, bed.id, page, signal), [garden, bed.id, page])
  const result = useRemote(`${garden}/${bed.id}/${page}/${revision}`, load)
  const canWrite = workspace.isActive && bed.isActive
  const canStart = canWrite && !!bed.configuration
  const bedHref = (query: Record<string, string>) => routeHref(garden, 'horta', 'canteiros', bed.id, new URLSearchParams(query))
  return <section className="my-8 border-t border-horta-line pt-6" aria-label="Cultivos deste canteiro">
    <div className="mb-5 flex flex-wrap items-center justify-between gap-4"><div><h2 className="mb-2">O que está plantado aqui?</h2><p className="mb-0 text-horta-muted">Cadastre o cultivo e acompanhe os plantios deste canteiro.</p></div>{canStart && <a className="action-link" href={bedHref({ cultivate: '1' })}>Adicionar cultivo</a>}</div>
    {!bed.configuration && canWrite && <p className="notice">Complete a área do canteiro para cadastrar o que será plantado nele.</p>}
    {result.loading && <p role="status">Carregando cultivos…</p>}
    {result.error && <Failure error={result.error} retry={() => setRevision(value => value + 1)} />}
    {result.data && (result.data.items.length ? <ul className="space-y-4">{result.data.items.map(cycle => <li key={cycle.id} className="rounded-lg border border-horta-line p-5">
      <div className="flex flex-wrap items-start justify-between gap-3"><div><h3 className="mt-0 mb-2 text-lg">{cycle.speciesName}</h3><p className="text-horta-muted">{formatNumber(cycle.occupiedAreaM2)} m² · Início em {formatDate(cycle.startedOn)}</p></div><span className="badge">{cycle.status === 'Closed' ? 'Cultivo encerrado' : cycle.plantings.totalCount ? 'Com plantio registrado' : 'Plantio ainda não registrado'}</span></div>
      {!!cycle.plantings.items.length && <ul className="mb-4 space-y-2">{cycle.plantings.items.map(planting => <li key={planting.id} className="text-sm"><a className="underline underline-offset-4" href={routeHref(garden, 'cultivo', 'plantios', planting.id, new URLSearchParams({ returnBedId: bed.id }))}>{formatNumber(planting.plantedQuantity)} plantas · {formatDate(planting.plantedOn)}{planting.kind === 'Replacement' ? ' · Reposição' : ''}</a></li>)}</ul>}
      <div className="flex flex-wrap items-center gap-x-5 gap-y-3 text-sm font-semibold">
        {canWrite && cycle.status === 'InProgress' && <a className="underline underline-offset-4" href={bedHref({ plantingCycle: cycle.id })}>{cycle.plantings.totalCount ? 'Registrar outro plantio' : 'Registrar plantio'}</a>}
        <a className="underline underline-offset-4" href={routeHref(garden, 'cultivo', 'ciclos', cycle.id, new URLSearchParams({ returnBedId: bed.id }))}>Detalhes do cultivo</a>
        {cycle.plantings.hasNextPage && <a className="underline underline-offset-4" href={routeHref(garden, 'cultivo', 'plantios', '', new URLSearchParams({ cycleId: cycle.id, bedId: bed.id, from: '0001-01-01', to: '9998-12-31', returnBedId: bed.id }))}>Ver todos os plantios ({cycle.plantings.totalCount})</a>}
      </div>
    </li>)}</ul> : <div className="rounded-lg bg-horta-canvas/70 p-6"><h3 className="mt-0">Nenhum cultivo cadastrado</h3><p className="mb-0 text-horta-muted">Use Adicionar cultivo para informar o que será plantado, a data de início e a área ocupada. A quantidade é registrada quando o plantio acontecer.</p></div>)}
    {result.data && result.data.totalPages > 1 && <Pagination data={result.data} change={setPage} label="Páginas de cultivos do canteiro" />}
  </section>
}
