import type { ReactNode } from 'react'

/** Cabeçalho único das páginas: contexto, título, situação, resumo e ações, sempre na mesma ordem. */
export function PageHeader({ eyebrow, title, status, description, actions, headingLevel = 1 }: { eyebrow?: ReactNode; title: ReactNode; status?: ReactNode; description?: ReactNode; actions?: ReactNode; headingLevel?: 1 | 2 }) {
  const Heading = `h${headingLevel}` as 'h1' | 'h2'
  return <div className="page-heading">
    <div>
      {eyebrow && <span className="eyebrow">{eyebrow}</span>}
      <div className="page-title"><Heading tabIndex={-1}>{title}</Heading>{status}</div>
      {description && <p>{description}</p>}
    </div>
    {actions && <div className="page-actions">{actions}</div>}
  </div>
}
