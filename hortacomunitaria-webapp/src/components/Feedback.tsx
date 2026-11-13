import { PageNavigator } from './PageNavigator'
import type { ApiFailure, Page } from '../types/api'

export function Failure({ error, retry, focusField }: { error: ApiFailure; retry?: () => void; focusField?: (name: string) => void }) {
  return <div className="feedback error" role="alert"><strong>Não foi possível concluir</strong><p>{error.message}</p>
    {Object.keys(error.fields).length > 0 && <ul>{Object.entries(error.fields).map(([field, messages]) => <li key={field}>{focusField ? <button type="button" className="feedback-field" onClick={() => focusField(field)}>{messages.join(' ')}</button> : messages.join(' ')}</li>)}</ul>}
    {error.traceId && <details><summary>Referência para suporte</summary><code>{error.traceId}</code>{error.code && <p>{error.code}</p>}</details>}
    {retry && <button type="button" className="secondary" onClick={retry}>Tentar novamente</button>}</div>
}
/** Paginação de listas e históricos. Com tamanho e total disponíveis, segue o padrão visual do PageNavigator. */
export function Pagination({ data, change, label }: { data: Pick<Page<unknown>, 'page' | 'totalPages' | 'hasPreviousPage' | 'hasNextPage'> & Partial<Pick<Page<unknown>, 'pageSize' | 'totalCount'>>; change: (page: number) => void; label: string }) {
  if (data.totalPages <= 1 && !data.hasPreviousPage && !data.hasNextPage) return null
  if (data.pageSize !== undefined && data.totalCount !== undefined) return <PageNavigator className="pagination" data={{ page: data.page, pageSize: data.pageSize, totalCount: data.totalCount, totalPages: data.totalPages }} change={change} label={label} />
  return <nav className="pagination" aria-label={label}><button className="secondary" disabled={!data.hasPreviousPage} onClick={() => change(data.page - 1)}>Anterior</button>
    <span>Página {data.page} de {Math.max(1, data.totalPages)}</span><button className="secondary" disabled={!data.hasNextPage} onClick={() => change(data.page + 1)}>Próxima</button></nav>
}
