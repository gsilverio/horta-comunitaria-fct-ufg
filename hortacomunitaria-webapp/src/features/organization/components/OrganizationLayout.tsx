import type { ReactNode } from 'react'
import { Icon } from '../../../components/Icon'
import { Failure, Pagination } from '../../../components/Feedback'
import { routeHref } from '../../../hooks/useRoute'
import { useGardenState } from '../../../hooks/useGardenState'
import type { ApiFailure, Page } from '../../../types/api'

export function OrganizationTrail({ sector, current }: { sector?: { id: string; name: string }; current?: string }) {
  const garden = useGardenState()
  return <nav aria-label="Você está em" className="organization-trail"><ol>
    <li>{garden.id || current ? <a href={routeHref('', 'horta', 'hortas')}>Hortas</a> : <span aria-current="page">Hortas</span>}</li>
    {garden.id && <li><Icon name="chevron" />{sector || current ? <a href={routeHref(garden.id, 'horta', 'setores')}>{garden.name}</a> : <span aria-current="page">{garden.name}</span>}</li>}
    {sector && <li><Icon name="chevron" />{current ? <a href={routeHref(garden.id, 'horta', 'setores', sector.id)}>{sector.name}</a> : <span aria-current="page">{sector.name}</span>}</li>}
    {current && <li><Icon name="chevron" /><span aria-current="page">{current}</span></li>}
  </ol></nav>
}

export function OrganizationHeading({ eyebrow, title, description, action }: { eyebrow: string; title: string; description: string; action?: ReactNode }) {
  return <div className="page-heading organization-heading"><div><span className="eyebrow">{eyebrow}</span><h1 tabIndex={-1}>{title}</h1><p>{description}</p></div>{action}</div>
}

export function CreateLink({ href, children }: { href: string; children: ReactNode }) {
  return <a className="action-link gap-2" href={href}><Icon name="plus" />{children}</a>
}

export function OrganizationCollection({ title, loading, error, retry, data, changePage, children, emptyTitle, emptyText, emptyAction, filter }: {
  title: string; loading: boolean; error?: ApiFailure; retry: () => void; data?: Page<unknown>; changePage: (page: number) => void; children: ReactNode;
  emptyTitle: string; emptyText: string; emptyAction?: ReactNode; filter?: ReactNode;
}) {
  return <section aria-label={title} aria-busy={loading} className="organization-collection">
    <div className="flex flex-wrap items-center justify-between gap-4 border-b border-horta-line px-5 py-4 sm:px-6"><h2 className="m-0 text-base">{title}{data && <span className="ml-2 font-normal text-horta-muted">({data.totalCount})</span>}</h2>{filter}</div>
    {loading && <p role="status" className="p-6">Carregando {title.toLocaleLowerCase('pt-BR')}…</p>}
    {error && <div className="p-5"><Failure error={error} retry={retry} /></div>}
    {data && !loading && !error && (data.items.length ? <ul className="organization-rows">{children}</ul> : <div className="empty py-12"><Icon name="cultivo" /><h3>{emptyTitle}</h3><p className="mx-auto max-w-md">{emptyText}</p>{emptyAction}</div>)}
    {data && !loading && !error && data.totalPages > 1 && <div className="border-t border-horta-line px-5 pb-5"><Pagination data={data} change={changePage} label={`Páginas de ${title}`} /></div>}
  </section>
}

export function OrganizationRow({ title, description, href, action, status, secondaryHref, actions }: { title: string; description: string; href: string; action: string; status?: string; secondaryHref?: string; actions?: ReactNode }) {
  return <li><div className="min-w-0"><h3><a href={href}>{title}</a></h3><p>{description}</p>{status && <span className="badge mt-2">{status}</span>}</div><div className="flex min-w-0 flex-wrap items-center gap-x-5 gap-y-2 sm:max-w-[60%] sm:justify-end">{actions}{secondaryHref && <a className="inline-flex min-h-11 items-center text-sm font-medium underline underline-offset-4" href={secondaryHref} aria-label={`Ver setores: ${title}`}>Ver setores</a>}<a className="organization-row-action" href={href} aria-label={`${action}: ${title}`}>{action}<Icon name="chevron" /></a></div></li>
}
