import { goToSame } from '../../../routes/navigation'
import type { AppRoute } from '../../../routes/routes'
import { useState } from 'react'

export function useOrganizationList(route: AppRoute) {
  const [revision, setRevision] = useState(0)
  const rawPage = Number(route.search.get('page'))
  const page = Number.isInteger(rawPage) && rawPage > 0 ? rawPage : 1
  const status = route.search.get('status') ?? 'active'
  const rawPageSize = Number(route.search.get('pageSize'))
  const pageSize = [10, 25, 50, 100].includes(rawPageSize) ? rawPageSize : 10
  function change(key: string, value: string) {
    const search = new URLSearchParams(route.search)
    search.set(key, value)
    if (key !== 'page') search.delete('page')
    goToSame(route, route.id, search)
  }
  return { page, pageSize, status, isActive: status === 'all' ? undefined : status !== 'inactive', revision,
    retry: () => setRevision(value => value + 1), changePage: (value: number) => change('page', String(value)), changePageSize: (value: number) => change('pageSize', String(value)), changeStatus: (value: string) => change('status', value) }
}
