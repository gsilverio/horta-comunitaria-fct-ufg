import { useLayoutEffect, useRef } from 'react'
import { Lock } from 'lucide-react'
import type { ReactNode } from 'react'
import { areaHomePage, areaOf, linkTo, navigationGroups, pageMeta } from '../routes/routes'
import type { AppRoute, PageKey } from '../routes/routes'
import { Icon } from './Icon'
import { terrainBlocks, useTerrainRequirement } from '../features/organization/hooks/useTerrainRequirement'

/** Altura mínima da lista de páginas (três itens) antes de o menu inteiro passar a rolar. */
const MIN_PAGES_HEIGHT = 120

/** Filtros que acompanham a troca de página dentro da mesma área. */
function queryFor(route: AppRoute, target: PageKey) {
  const query = new URLSearchParams()
  if (areaOf(route.page)?.id === 'visao-geral' && target.startsWith('visao-geral/')) for (const key of ['from', 'to']) { const value = route.search.get(key); if (value) query.set(key, value) }
  if (target === 'catalogo/cultivares') {
    const species = route.page === 'catalogo/especies' && route.id && route.id !== 'novo' ? route.id : route.search.get('speciesId')
    if (species) query.set('speciesId', species)
  }
  return query
}

/** `garden` é a horta de contexto: em catálogos e na lista de hortas, os links das áreas continuam nela. */
export function Navigation({ route, onNavigate, gardenSwitcher }: { route: AppRoute; onNavigate?: () => void; gardenSwitcher?: ReactNode }) {
  const close = (event: React.MouseEvent) => { if (!event.defaultPrevented) onNavigate?.() }
  // As páginas da área aberta ocupam só o espaço livre do menu e rolam internamente, para que todas as áreas
  // continuem visíveis. Abaixo de três itens de altura, o menu inteiro volta a rolar.
  const pagesRef = useRef<HTMLUListElement>(null)
  const area = areaOf(route.page)?.id
  useLayoutEffect(() => {
    const list = pagesRef.current, container = list?.closest<HTMLElement>('.workspace-sidebar, .navigation-dialog')
    if (!list || !container) return
    const fit = () => {
      if (!container.clientHeight) return
      list.style.maxHeight = ''
      const others = container.scrollHeight - list.offsetHeight
      list.style.maxHeight = `${Math.max(MIN_PAGES_HEIGHT, container.clientHeight - others)}px`
      const item = list.querySelector<HTMLElement>('[aria-current=page]')
      if (!item) return
      const top = item.offsetTop - list.offsetTop, bottom = top + item.offsetHeight
      if (top < list.scrollTop || bottom > list.scrollTop + list.clientHeight) list.scrollTop = top - (list.clientHeight - item.offsetHeight) / 2
    }
    fit()
    const observer = new ResizeObserver(fit)
    observer.observe(container)
    return () => observer.disconnect()
  }, [area, route.page, route.garden])
  const current = areaOf(route.page)?.id ?? (route.scope === 'gardens' ? 'horta' : undefined)
  // Com a área Horta aberta, Setores e Canteiros mostram cadeado enquanto a horta não tem terreno.
  const terrain = useTerrainRequirement(current === 'horta' && route.scope === 'garden' ? route.garden : '', `${route.page}/${route.id}`)
  const locked = terrainBlocks(terrain.data)
  const preserve = route.page === 'horta/mapa' ? 'true' : undefined
  return <nav className="primary-navigation" aria-label="Áreas da aplicação">
    {gardenSwitcher && <div className="nav-garden">{gardenSwitcher}</div>}
    {navigationGroups.map(group => <div className="nav-group" key={group.label}>
      <p className="nav-group-title">{group.label}</p>
      {group.areas.map(area => {
        const open = current === area.id
        const pages = open && route.garden && area.pages.length > 1 ? area.pages : []
        const single = area.pages.length === 1 && route.page === area.pages[0]
        // Horta abre a gestão de hortas; as demais áreas abrem sua página inicial na horta de contexto.
        const href = area.id === 'horta' || !route.garden ? linkTo('', 'hortas') : linkTo(route.garden, areaHomePage(area.id))
        return <div key={area.id}>
          <a className="nav-link" aria-current={single || (open && route.scope === 'gardens') ? 'page' : undefined} data-open={open} data-preserve-draft={area.id === 'visao-geral' ? preserve : undefined} href={href} onClick={close}>
            <Icon name={area.id} /><span>{area.label}</span>
          </a>
          {pages.length > 0 && <ul ref={pagesRef} className="nav-pages" aria-label={`Páginas de ${area.label}`}>
            {pages.map(key => {
              const blocked = locked && (key === 'horta/setores' || key === 'horta/canteiros')
              return <li key={key}><a className="nav-page" data-locked={blocked || undefined} aria-current={key === route.page ? 'page' : undefined} href={linkTo(route.garden, key, '', queryFor(route, key))} onClick={close} title={blocked ? 'Desenhe o terreno no mapa primeiro' : undefined}>{pageMeta(key)?.label}{blocked && <><Lock aria-hidden="true" className="nav-page-lock" /><span className="sr-only"> (requer terreno)</span></>}</a></li>
            })}
          </ul>}
        </div>
      })}
    </div>)}
  </nav>
}
