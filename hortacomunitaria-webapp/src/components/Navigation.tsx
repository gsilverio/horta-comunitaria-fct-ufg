import { routeHref } from '../hooks/useRoute'
import type { RecordRoute } from '../types/records'
import { modulePages, navigationItems } from './navigationItems'
import { Icon } from './Icon'

const groups = [
  { label: 'Acompanhamento', ids: ['visao-geral'] },
  { label: 'Organização', ids: ['horta', 'atividades', 'ocorrencias', 'custos'] },
  { label: 'No campo', ids: ['irrigacao', 'ambiente', 'cultivo', 'producao', 'fotos'] },
]

function queryFor(route: RecordRoute, resource: string) {
  const query = new URLSearchParams()
  if (route.module === 'visao-geral') for (const key of ['from', 'to']) { const value = route.search.get(key); if (value) query.set(key, value) }
  if (route.module === 'cultivo' && ['cultivares', 'origens'].includes(resource)) {
    const species = route.resource === 'especies' && route.id && route.id !== 'novo' ? route.id : route.search.get('speciesId')
    if (species) query.set('speciesId', species)
  }
  return query
}

export function Navigation({ route, onNavigate }: { route: RecordRoute; onNavigate?: () => void }) {
  const close = (event: React.MouseEvent) => { if (!event.defaultPrevented) onNavigate?.() }
  const activeModule = route.garden ? route.module : route.search.get('nextModule') || 'horta'
  const preserve = (id: string) => route.module === 'horta' && route.resource === 'interativa' && id === 'visao-geral' ? 'true' : undefined
  return <nav className="primary-navigation" aria-label="Áreas da aplicação">
    {groups.map(group => <div className="nav-group" key={group.label}>
      <p className="nav-group-title">{group.label}</p>
      {group.ids.map(id => {
        const item = navigationItems.find(value => value.id === id)
        if (!item) return null
        const open = activeModule === item.id
        const pages = open && route.garden ? modulePages(item.id) : []
        const isCatalog = item.id === 'horta' && route.resource === 'hortas' && !route.id
        const current = open && (pages.length === 0 || isCatalog)
        const href = item.id === 'horta' ? routeHref(route.garden, 'horta', 'hortas') : route.garden ? routeHref(route.garden, item.id, item.items[0].id) : routeHref('', 'horta', 'hortas', '', new URLSearchParams({ nextModule: item.id }))
        return <div key={item.id}>
          <a className="nav-link" aria-current={current ? 'page' : undefined} data-open={open} data-preserve-draft={preserve(item.id)} href={href} onClick={close}>
            <Icon name={item.id} /><span>{item.label}</span>
          </a>
          {pages.length > 0 && <ul className="nav-pages" aria-label={`Páginas de ${item.label}`}>
            {pages.map(page => {
              const pageCurrent = page.id === route.resource && !isCatalog
              return <li key={page.id}><a className="nav-page" aria-current={pageCurrent ? 'page' : undefined} href={routeHref(route.garden, item.id, page.id, item.id === 'horta' && page.id === 'hortas' ? route.garden : '', queryFor(route, page.id))} onClick={close}>{page.label}</a></li>
            })}
          </ul>}
        </div>
      })}
    </div>)}
  </nav>
}
