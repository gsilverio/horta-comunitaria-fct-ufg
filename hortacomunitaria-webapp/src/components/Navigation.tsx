import { routeHref } from '../hooks/useRoute'
import type { RecordRoute } from '../types/records'
import { navigationItems } from './navigationItems'
import { Icon } from './Icon'

const groups = [
  { label: 'Acompanhamento', ids: ['visao-geral'] },
  { label: 'Organização', ids: ['horta', 'atividades', 'ocorrencias', 'custos'] },
  { label: 'No campo', ids: ['irrigacao', 'ambiente', 'cultivo', 'producao'] },
]
export function Navigation({ route, onNavigate }: { route: RecordRoute; onNavigate?: () => void }) {
  return <nav className="primary-navigation" aria-label="Áreas da aplicação">
    {groups.map(group => <div className="nav-group" key={group.label}>
      <p className="nav-group-title">{group.label}</p>
      {group.ids.map(id => {
        const item = navigationItems.find(value => value.id === id)
        if (!item) return null
        const activeModule = route.garden ? route.module : route.search.get('nextModule') || 'horta'
        const href = route.garden ? routeHref(route.garden, item.id, item.id === 'horta' ? 'interativa' : item.items[0].id) : routeHref('', 'horta', 'hortas', '', item.id === 'horta' ? undefined : new URLSearchParams({ nextModule: item.id }))
        return <a key={item.id} className="nav-link" aria-current={activeModule === item.id ? 'page' : undefined}
          data-preserve-draft={route.module === 'horta' && route.resource === 'interativa' && item.id === 'visao-geral' ? 'true' : undefined}
          href={href} onClick={event => { if (!event.defaultPrevented) onNavigate?.() }}>
          <Icon name={item.id} /><span>{item.label}</span><Icon name="chevron" className="nav-chevron" />
        </a>
      })}
    </div>)}
  </nav>
}
export function SectionNavigation({ route }: { route: RecordRoute }) {
  const current = navigationItems.find(item => item.id === route.module)
  if (!current || current.items.length < 2) return null
  const period = new URLSearchParams()
  if (route.module === 'visao-geral') for (const key of ['from', 'to']) { const value = route.search.get(key); if (value) period.set(key, value) }
  return <nav className="section-navigation" aria-label="Seções da área">
    {current.items.map(item => <a key={item.id} aria-current={route.resource === item.id || !route.resource && current.items[0].id === item.id ? 'page' : undefined}
      href={item.id === "hortas" ? routeHref(route.garden, "horta", "hortas", route.garden) : routeHref(route.garden, current.id, item.id, "", period)}>{item.id === "hortas" ? "Dados da horta" : item.label}</a>)}
  </nav>
}
