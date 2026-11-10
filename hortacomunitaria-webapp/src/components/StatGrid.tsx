import type { ReactNode } from 'react'

export interface Stat { label: string; value: ReactNode; unit?: string; hint?: ReactNode }

/** Resumo numérico: rótulo, valor em destaque, unidade e contexto em uma grade única. */
export function StatGrid({ items, label }: { items: Stat[]; label: string }) {
  return <dl className="stat-grid" aria-label={label}>
    {items.map(item => <div className="stat" key={item.label}>
      <dt className="stat-label">{item.label}</dt>
      <dd className="stat-value">{item.value}{item.unit && <small>{item.unit}</small>}</dd>
      {item.hint && <dd className="stat-hint">{item.hint}</dd>}
    </div>)}
  </dl>
}
