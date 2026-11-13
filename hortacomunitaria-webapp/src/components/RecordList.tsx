import { Icon } from './Icon'
import { useGardenState } from "../hooks/useGardenState"
import type { ReactNode } from 'react'
import { useCanManage } from '../features/auth/hooks/useAuth'
import type { ApiFailure, Page } from '../types/api'
import { Failure } from './Feedback'
import { PageNavigator } from './PageNavigator'
import { FilterBar } from './FilterBar'
import { PageHeader } from './PageHeader'
export function RecordList({ title, hint, create, children, filters, apply, loading, error, retry, data, page, requireActiveGarden = true, createDisabled = false }: { title: string; hint: string; create: () => void; children: ReactNode; filters: ReactNode; apply: () => void; loading: boolean; error?: ApiFailure; retry: () => void; data?: Pick<Page<unknown>, 'page' | 'pageSize' | 'totalPages' | 'totalCount' | 'hasPreviousPage' | 'hasNextPage'>; page: (page: number) => void; requireActiveGarden?: boolean; createDisabled?: boolean }) {
  const garden = useGardenState()
  // Perfil Operador consulta catálogos e estrutura sem a ação de criação.
  const canManage = useCanManage()
  return <>
    <PageHeader title={title} description={hint} actions={canManage && <button className="flex items-center justify-center gap-2" disabled={createDisabled || requireActiveGarden && !garden.isActive} onClick={create}><Icon name="plus" />Novo registro</button>} />
    <FilterBar label={`Filtros de ${title}`} apply={apply} hasError={!!error && Object.keys(error.fields).length > 0}>{filters}</FilterBar>
    <section className="panel flush" aria-busy={loading}>
      <div className="results-heading"><h2>Registros</h2><span className="text-xs text-horta-muted">{data ? `${data.totalCount} no recorte` : '—'}</span></div>
      {loading && <p role="status" className="panel-body plant-status">Carregando registros…</p>}
      {error && <div className="panel-body"><Failure error={error} retry={retry} /></div>}
      {data && <>{data.totalCount === 0 ? <div className="empty"><Icon name="records" /><h3>Nenhum registro neste recorte</h3><p className="mb-0">Ajuste os filtros ou adicione o primeiro registro.</p></div> : <ul className="record-results">{children}</ul>}{data.totalPages > 1 && <PageNavigator className="org-pagination" data={data} change={page} label={`Páginas de ${title}`} />}</>}
    </section>
  </>
}
