import type { ReactNode } from 'react'
import { Icon } from './Icon'

/** Linha padrão de listagem: a linha inteira abre o registro. */
export function RecordRow({ href, title, meta, description, status, statusTone }: { href: string; title: ReactNode; meta?: ReactNode; description?: ReactNode; status?: string; statusTone?: 'warning' | 'invalid' }) {
  return <li className="record-row">
    <a href={href}>{title}</a>
    {description && <small className="record-description">{description}</small>}
    <span className="record-meta">{meta}{status && <span className={`badge ${statusTone ?? ''}`}>{status}</span>}<Icon name="chevron" className="size-3.5 text-horta-muted" /></span>
  </li>
}
