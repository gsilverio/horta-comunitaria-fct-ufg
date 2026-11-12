import { ArrowLeft, ChevronRight, Sprout } from 'lucide-react'
import { useBackLink } from '../hooks/useBackLink'
import type { BackLinkTarget } from '../hooks/useBackLink'
import { areaHomePage, areaOf, linkTo, pageMeta } from '../routes/routes'
import type { AppRoute } from '../routes/routes'

export interface TrailItem { label: string; href?: string }

/** Retorno à página de origem. `standalone` marca o uso fora da barra, ocultado quando a barra já o exibe. */
export function BackLink({ target, standalone = false }: { target: BackLinkTarget; standalone?: boolean }) {
  return <a className={`page-back${standalone ? ' page-back-standalone' : ''}`} href={target.href} title={`Voltar: ${target.label}`}>
    <ArrowLeft aria-hidden="true" className="size-4 shrink-0" />
    <span className="sr-only">Voltar: </span><span className="truncate">{target.label}</span>
  </a>
}

/**
 * Barra de contexto da página: retorno (quando houver) e caminho, na mesma linha. Itens anteriores são links;
 * o último é a página atual.
 */
export function PageTrail({ items }: { items: TrailItem[] }) {
  const back = useBackLink()
  if (items.length === 0 && !back) return null
  return <div className="page-context">
    {back && <BackLink target={back} />}
    {back && items.length > 0 && <span aria-hidden="true" className="page-context-divider" />}
    {items.length > 0 && <nav aria-label="Você está em" className="page-trail"><ol>
      {items.map((item, index) => {
        const last = index === items.length - 1
        return <li key={`${index}/${item.label}`}>
          {index === 0 && <Sprout aria-hidden="true" className="page-trail-home" />}
          {index > 0 && <ChevronRight aria-hidden="true" className="page-trail-separator" />}
          {last || !item.href ? <span aria-current={last ? 'page' : undefined} title={item.label}>{item.label}</span> : <a href={item.href} title={item.label}>{item.label}</a>}
        </li>
      })}
    </ol></nav>}
  </div>
}

const actionLabels = { editar: 'Correção', configurar: 'Configuração', cultivar: 'Novo cultivo', plantar: 'Plantio', inativar: 'Inativação', fases: 'Fases visuais' } as const

/** Caminho genérico das áreas (horta → área → página → registro). Catálogos não exibem a horta. */
export function LocationTrail({ route, gardenName }: { route: AppRoute; gardenName?: string }) {
  const area = areaOf(route.page), page = pageMeta(route.page)
  const items: TrailItem[] = []
  if (gardenName && route.scope === 'garden') items.push({ label: gardenName, href: linkTo(route.garden, 'visao-geral') })
  if (area) items.push({ label: area.label, href: linkTo(route.garden, area.id === 'horta' ? 'horta/setores' : areaHomePage(area.id)) })
  if (page && route.page && page.label !== area?.label) items.push({ label: page.label, href: linkTo(route.garden, route.page) })
  if (route.id) items.push({ label: route.id === 'novo' ? 'Novo registro' : route.action ? actionLabels[route.action] : 'Detalhe' })
  else if (route.action) items.push({ label: actionLabels[route.action] })
  return <PageTrail items={items} />
}
