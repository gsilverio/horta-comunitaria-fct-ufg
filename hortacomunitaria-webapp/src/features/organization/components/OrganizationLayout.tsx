import { linkTo } from '../../../routes/routes'
import type { ReactNode } from 'react'
import { Icon } from '../../../components/Icon'
import { Failure, Pagination } from '../../../components/Feedback'
import { PageHeader } from '../../../components/PageHeader'
import { PageTrail } from '../../../components/PageTrail'
import type { TrailItem } from '../../../components/PageTrail'
import { useGardenState } from '../../../hooks/useGardenState'
import { useCanManage } from '../../auth/hooks/useAuth'
import type { ApiFailure, Page } from '../../../types/api'

export function OrganizationTrail({ sector, current, withGarden = true }: { sector?: { id: string; name: string }; current?: string; withGarden?: boolean }) {
  const workspace = useGardenState(), garden = withGarden ? workspace : { ...workspace, id: '' }
  // Sem horta nem página atual, a barra mostra apenas o retorno, se houver.
  if (!garden.id && !current) return <PageTrail items={[]} />
  const items: TrailItem[] = [{ label: 'Hortas', href: linkTo('', 'hortas') }]
  if (garden.id) items.push({ label: garden.name, href: linkTo(garden.id, 'horta/setores') })
  if (sector) items.push({ label: sector.name, href: linkTo(garden.id, 'horta/setores', sector.id) })
  if (current) items.push({ label: current })
  return <PageTrail items={items} />
}

export function OrganizationHeading({ eyebrow, title, description, action }: { eyebrow?: string; title: string; description: string; action?: ReactNode }) {
  return <PageHeader eyebrow={eyebrow} title={title} description={description} actions={action} />
}

export function CreateLink({ href, children }: { href: string; children: ReactNode }) {
  // Criação de hortas, setores e canteiros é exclusiva do perfil Adm.
  if (!useCanManage()) return null
  return <a className="action-link" href={href}><Icon name="plus" />{children}</a>
}

export function OrganizationCollection({ title, loading, error, retry, data, changePage, children, emptyTitle, emptyText, emptyAction, filter, action }: {
  title: string; loading: boolean; error?: ApiFailure; retry: () => void; data?: Page<unknown>; changePage: (page: number) => void; children: ReactNode;
  emptyTitle: string; emptyText: string; emptyAction?: ReactNode; filter?: ReactNode; action?: ReactNode;
}) {
  return <section aria-label={title} aria-busy={loading} className="organization-collection panel flush">
    <div className="organization-collection-heading"><h2 className="m-0 text-sm">{title}{data && <span className="ml-2 font-normal text-horta-muted">({data.totalCount})</span>}</h2><div className="flex flex-wrap items-center gap-3">{filter}{action}</div></div>
    {loading && <p role="status" className="panel-body plant-status">Carregando {title.toLocaleLowerCase('pt-BR')}…</p>}
    {error && <div className="panel-body"><Failure error={error} retry={retry} /></div>}
    {data && !loading && !error && (data.items.length ? <ul className="organization-rows">{children}</ul> : <div className="empty"><Icon name="cultivo" /><h3>{emptyTitle}</h3><p className="mx-auto mb-0 max-w-md">{emptyText}</p>{emptyAction}</div>)}
    {data && !loading && !error && <Pagination data={data} change={changePage} label={`Páginas de ${title}`} />}
  </section>
}

export function OrganizationRow({ title, description, href, action, status, statusTone, secondaryHref, actions }: { title: string; description: string; href: string; action: string; status?: string; statusTone?: 'warning' | 'invalid'; secondaryHref?: string; actions?: ReactNode }) {
  return <li>
    <div className="min-w-0"><div className="flex flex-wrap items-center gap-2"><h3><a href={href}>{title}</a></h3>{status && <span className={`badge ${statusTone ?? ''}`}>{status}</span>}</div><p>{description}</p></div>
    <div className="organization-row-actions">{actions}{secondaryHref && <a className="organization-row-action underline underline-offset-4" href={secondaryHref} aria-label={`Ver setores: ${title}`}>Ver setores</a>}<a className="organization-row-action" href={href} aria-label={`${action}: ${title}`}>{action}<Icon name="chevron" /></a></div>
  </li>
}
