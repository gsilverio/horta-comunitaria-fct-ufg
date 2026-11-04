import { useState } from 'react'
import type { RecordRoute } from '../types/records'
import { navigate } from './useRoute'
import { today } from '../features/irrigation/formValues'
import type { ApiFailure } from '../types/api'
export function useListFilters(route: RecordRoute, fields: readonly string[], fact: boolean) {
  const initial: Record<string, string> = {}
  for (const field of fields) initial[field] = route.search.get(field) ?? (field === 'from' ? today().slice(0, 7) + '-01' : field === 'to' ? today() : field === 'validity' ? 'Valid' : '')
  const key = route.search.toString()
  const [draftState, setDraft] = useState({ key, values: initial }), [revision, setRevision] = useState(0), [error, setError] = useState<ApiFailure>()
  const draft = draftState.key === key ? draftState.values : initial
  const values: Record<string, string> = {}
  for (const field of fields) values[field] = route.search.get(field) ?? initial[field]
  const page = Math.max(1, Number(route.search.get('page')) || 1)
  function go(newValues: Record<string, string>, targetPage: number) { const query = new URLSearchParams(); for (const [key, value] of Object.entries(newValues)) if (value) query.set(key, value); query.set('page', String(targetPage)); navigate(route.garden, route.module, route.resource, '', query) }
  return { draft, values, page, revision, error, change: (field: string, value: string) => { setDraft(x => ({ key, values: { ...(x.key === key ? x.values : initial), [field]: value } })); setError(undefined) }, apply: () => { if (fact && (!draft.from || !draft.to || draft.from > draft.to)) { setError({ message: 'Revise o período.', fields: { from: ['O início deve ser anterior ou igual ao fim.'], to: ['Revise o intervalo informado.'] } }); return }; setError(undefined); go(draft, 1); setRevision(x => x + 1) }, changePage: (page: number) => go(values, page), retry: () => setRevision(x => x + 1) }
}
