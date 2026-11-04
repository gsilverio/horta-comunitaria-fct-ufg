import { useState } from 'react'
import { navigate } from '../../../hooks/useRoute'
import type { RecordRoute } from '../../../types/records'

export function useOrganizationList(route: RecordRoute) {
  const [revision, setRevision] = useState(0)
  const rawPage = Number(route.search.get('page'))
  const page = Number.isInteger(rawPage) && rawPage > 0 ? rawPage : 1
  const status = route.search.get('status') ?? 'active'
  function change(key: string, value: string) {
    const search = new URLSearchParams(route.search)
    search.set(key, value)
    if (key !== 'page') search.delete('page')
    navigate(route.garden, route.module, route.resource, route.id, search)
  }
  return { page, status, isActive: status === 'all' ? undefined : status !== 'inactive', revision,
    retry: () => setRevision(value => value + 1), changePage: (value: number) => change('page', String(value)), changeStatus: (value: string) => change('status', value) }
}
