import { readRoute, routeHref } from '../hooks/useRoute'

/** Retém somente o contexto de consulta para voltar ao indicador depois de revisar a origem. */
export function indicatorOriginHref(garden: string, module: string, resource: string, id: string, filters?: URLSearchParams) {
  const current = readRoute()
  const query = new URLSearchParams(filters)
  if (current.module === 'visao-geral' && current.garden === garden) {
    query.set('returnTo', routeHref(garden, current.module, current.resource, '', current.search))
  }
  return routeHref(garden, module, resource, id, query)
}
