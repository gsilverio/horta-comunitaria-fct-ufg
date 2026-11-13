import { linkTo } from '../../../routes/routes'
import type { ReactNode } from 'react'
import { Icon } from '../../../components/Icon'
import { ChevronRight, Fence, LayoutGrid, Sprout } from 'lucide-react'
import { Failure } from '../../../components/Feedback'
import { PageNavigator } from '../../../components/PageNavigator'
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

export function CreateLink({ href, children, compact = false }: { href: string; children: ReactNode; compact?: boolean }) {
  // Criação de hortas, setores e canteiros é exclusiva do perfil Adm.
  if (!useCanManage()) return null
  return <a className={compact ? 'action-link org-create-compact' : 'action-link'} href={href}><Icon name="plus" />{children}</a>
}

export function OrganizationCollection({ title, loading, error, retry, data, changePage, children, emptyTitle, emptyText, emptyAction, filter, action }: {
  title: string; loading: boolean; error?: ApiFailure; retry: () => void; data?: Page<unknown>; changePage: (page: number) => void; children: ReactNode;
  emptyTitle: string; emptyText: string; emptyAction?: ReactNode; filter?: ReactNode; action?: ReactNode;
}) {
  return <section aria-label={title} aria-busy={loading} className="org-collection">
    <div className="org-collection-heading">
      <h2>{title}{data && <span className="org-count">{data.totalCount}</span>}</h2>
      {(filter || action) && <div className="org-collection-tools">{filter}{action}</div>}
    </div>
    {loading && <p role="status" className="org-collection-status">Carregando {title.toLocaleLowerCase('pt-BR')}…</p>}
    {error && <div className="org-collection-status"><Failure error={error} retry={retry} /></div>}
    {data && !loading && !error && (data.items.length ? <ul className="org-rows">{children}</ul> : <div className="empty"><Icon name="cultivo" /><h3>{emptyTitle}</h3><p className="mx-auto mb-0 max-w-md">{emptyText}</p>{emptyAction}</div>)}
    {data && !loading && !error && data.totalPages > 1 && <PageNavigator className="org-pagination" data={data} change={changePage} label={`Páginas de ${title}`} />}
  </section>
}

/**
 * Linha de setor ou canteiro: ícone do tipo, nome, informações em etiquetas e a linha inteira como link.
 * `actions` é uma ação rápida opcional (ex.: Adicionar cultivo); `action` nomeia o link para leitores de tela.
 */
export function OrganizationRow({ title, description, meta = [], href, action, status, statusTone, secondaryHref, actions, kind = 'bed' }: {
  title: string; description?: string; meta?: string[]; href: string; action: string; status?: string; statusTone?: 'warning' | 'invalid'
  secondaryHref?: string; actions?: ReactNode; kind?: 'sector' | 'bed' | 'garden'
}) {
  const KindIcon = kind === 'sector' ? LayoutGrid : kind === 'garden' ? Fence : Sprout
  return <li className="org-row">
    <span className="org-row-icon" aria-hidden="true"><KindIcon /></span>
    <div className="org-row-main">
      <div className="org-row-title"><h3><a href={href} aria-label={`${action}: ${title}`}>{title}</a></h3>{status && <span className={`badge ${statusTone ?? ''}`}>{status}</span>}</div>
      {(meta.length > 0 || description) && <p className="org-row-meta">{meta.map(item => <span key={item}>{item}</span>)}{description && <span className="org-row-description">{description}</span>}</p>}
    </div>
    <div className="org-row-actions">
      {actions}
      {secondaryHref && <a className="org-row-quick" href={secondaryHref} aria-label={`Ver setores: ${title}`}>Setores</a>}
      <ChevronRight aria-hidden="true" className="org-row-chevron" />
    </div>
  </li>
}
