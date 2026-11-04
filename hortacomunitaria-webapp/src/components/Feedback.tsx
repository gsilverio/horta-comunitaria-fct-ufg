import type { ApiFailure, Page } from '../types/api'

export function Failure({ error, retry }: { error: ApiFailure; retry?: () => void }) {
  return <div className="feedback error" role="alert"><strong>Não foi possível concluir</strong><p>{error.message}</p>
    {Object.keys(error.fields).length > 0 && <ul>{Object.entries(error.fields).map(([field, messages]) => <li key={field}>{messages.join(' ')}</li>)}</ul>}
    {error.traceId && <details><summary>Referência para suporte</summary><code>{error.traceId}</code>{error.code && <p>{error.code}</p>}</details>}
    {retry && <button type="button" className="secondary" onClick={retry}>Tentar novamente</button>}</div>
}
export function Pagination({ data, change, label }: { data: Pick<Page<unknown>, 'page' | 'totalPages' | 'hasPreviousPage' | 'hasNextPage'>; change: (page: number) => void; label: string }) {
  return <nav className="pagination" aria-label={label}><button className="secondary" disabled={!data.hasPreviousPage} onClick={() => change(data.page - 1)}>Anterior</button>
    <span>Página {data.page} de {Math.max(1, data.totalPages)}</span><button className="secondary" disabled={!data.hasNextPage} onClick={() => change(data.page + 1)}>Próxima</button></nav>
}
