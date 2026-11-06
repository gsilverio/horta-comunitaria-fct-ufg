import { navigate, routeHref } from '../hooks/useRoute'
import type { KeyboardEvent } from 'react'
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
        const href = item.id === 'horta' ? routeHref('', 'horta', 'hortas') : route.garden ? routeHref(route.garden, item.id, item.items[0].id) : routeHref('', 'horta', 'hortas', '', new URLSearchParams({ nextModule: item.id }))
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
  if (!current || current.items.length < 2 || route.module === 'horta') return null
  const active = route.resource || current.items[0].id
  const groups = route.module === 'cultivo' ? [
    { label: 'Catálogo', items: current.items.filter(item => ['especies', 'cultivares', 'origens'].includes(item.id)) },
    { label: 'Acompanhamento', items: current.items.filter(item => !['especies', 'cultivares', 'origens'].includes(item.id)) },
  ] : [{ label: current.label, items: current.items }]
  function queryFor(resource: string) {
    const query = resource === active ? new URLSearchParams(route.search) : new URLSearchParams()
    for (const key of ['edit', 'visual', 'setup', 'cultivate', 'plantingCycle']) query.delete(key)
    if (route.module === 'visao-geral') for (const key of ['from', 'to']) { const value = route.search.get(key); if (value) query.set(key, value) }
    if (route.module === 'cultivo' && ['cultivares', 'origens'].includes(resource)) {
      const species = route.resource === 'especies' && route.id && route.id !== 'novo' ? route.id : route.search.get('speciesId')
      if (species) query.set('speciesId', species)
    }
    return query
  }
  function moveFocus(event: KeyboardEvent<HTMLElement>) {
    if (event.altKey || event.ctrlKey || event.metaKey || !['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return
    const links = Array.from(event.currentTarget.querySelectorAll<HTMLAnchorElement>('[data-section-link]'))
    const index = links.findIndex(link => link === event.target)
    if (index < 0) return
    const next = event.key === 'Home' ? 0 : event.key === 'End' ? links.length - 1 : (index + (event.key === 'ArrowRight' ? 1 : -1) + links.length) % links.length
    event.preventDefault()
    links[next]?.focus()
  }
  return <nav className="border-b border-horta-line bg-white px-4 py-4 sm:px-6 xl:px-9" aria-label={`Seções de ${current.label}`} onKeyDown={moveFocus}>
    <label className="gap-2 text-sm sm:hidden">Seção de {current.label}
      <select value={active} onChange={event => navigate(route.garden, current.id, event.target.value, '', queryFor(event.target.value))}>
        {groups.map(group => <optgroup key={group.label} label={group.label}>{group.items.map(item => <option key={item.id} value={item.id}>{item.label}</option>)}</optgroup>)}
      </select>
    </label>
    <div className="hidden flex-wrap items-end gap-x-8 gap-y-4 sm:flex">
      {groups.map(group => <div key={group.label} className="min-w-0">
        {groups.length > 1 && <p className="mb-2 text-[11px] font-semibold tracking-wide text-horta-muted uppercase">{group.label}</p>}
        <div className="flex flex-wrap gap-1">{group.items.map(item => <a key={item.id} data-section-link aria-current={active === item.id ? 'page' : undefined}
          className="inline-flex min-h-11 items-center rounded-md border border-transparent px-3 py-2 text-sm font-medium text-horta-muted transition-colors hover:bg-horta-canvas hover:text-horta-forest aria-[current=page]:border-horta-line aria-[current=page]:bg-horta-soft aria-[current=page]:font-semibold aria-[current=page]:text-horta-forest"
          href={routeHref(route.garden, current.id, item.id, '', queryFor(item.id))}>{item.label}</a>)}</div>
      </div>)}
    </div>
  </nav>
}
