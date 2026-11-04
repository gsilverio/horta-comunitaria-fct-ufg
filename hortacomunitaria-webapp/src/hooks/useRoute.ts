import { useEffect, useState } from 'react'
import type { RecordRoute } from '../types/records'
import { authorizeNavigation, releaseNavigationGuard } from '../services/navigationGuard'

export function readRoute(): RecordRoute {
  const [path, query = ''] = location.hash.slice(1).split('?')
  const parts = (path || '').split('/').filter(Boolean)
  return { garden: parts[0] === 'hortas' && parts[1] !== '_' ? parts[1] || '' : '', gardenSpecified: parts[0] === 'hortas' && !!parts[1], module: parts[2] || 'horta', resource: parts[3] || '', id: parts[4] || '', search: new URLSearchParams(query) }
}
export function routeHref(garden: string, module: string, resource = '', id = '', query?: URLSearchParams) {
  const path = ['hortas', garden || '_', module, resource, id].filter(Boolean).map(encodeURIComponent).join('/')
  return `#/${path}${query?.size ? `?${query}` : ''}`
}
export function navigate(garden: string, module: string, resource = '', id = '', query?: URLSearchParams) {
  if (!authorizeNavigation()) return
  location.hash = routeHref(garden, module, resource, id, query)
}
export function useRoute() {
  const [route, setRoute] = useState(readRoute)
  useEffect(() => {
    let previous = location.hash
    const changed = () => { if (!authorizeNavigation()) { history.replaceState(null, '', location.pathname + location.search + previous); return }; previous = location.hash; const current = readRoute(); setRoute(current); if (current.id !== 'novo' && current.search.get('edit') !== '1') requestAnimationFrame(() => document.querySelector<HTMLElement>('main h2, main h1')?.focus()) }
    const clicked = (event: MouseEvent) => {
      const link = event.target instanceof Element ? event.target.closest<HTMLAnchorElement>('a[href^="#/"]') : null
      if (!link) return
      if (link.dataset.preserveDraft === 'true') { releaseNavigationGuard(); return }
      if (!authorizeNavigation()) event.preventDefault()
    }
    window.addEventListener('hashchange', changed); document.addEventListener('click', clicked, true)
    return () => { window.removeEventListener('hashchange', changed); document.removeEventListener('click', clicked, true) }
  }, [])
  return route
}
