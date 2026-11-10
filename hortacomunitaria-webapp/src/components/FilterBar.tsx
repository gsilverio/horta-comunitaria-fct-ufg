import { useState } from 'react'
import type { ReactNode } from 'react'
import { FilterFields } from './Fields'
import { Icon } from './Icon'

/** Filtros aplicados pela API. No celular ficam recolhidos para que os resultados apareçam primeiro. */
export function FilterBar({ label, apply, children, hasError = false, submitLabel = 'Aplicar filtros' }: { label: string; apply: () => void; children: ReactNode; hasError?: boolean; submitLabel?: string }) {
  const [open, setOpen] = useState(false)
  const visible = open || hasError
  return <>
    <button type="button" className="secondary filter-toggle" aria-expanded={visible} onClick={() => setOpen(current => !current)}><Icon name="filter" />{visible ? 'Ocultar filtros' : 'Mostrar filtros'}</button>
    <form className="filter-panel" data-collapsed={!visible} aria-label={label} onSubmit={event => { event.preventDefault(); apply() }}>
      <FilterFields>{children}</FilterFields>
      <button className="secondary">{submitLabel}</button>
    </form>
  </>
}
