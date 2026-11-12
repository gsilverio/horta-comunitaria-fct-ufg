import { useId } from 'react'
import type { ReactNode, SelectHTMLAttributes } from 'react'

/**
 * Select com o rótulo dentro do controle, para barras e cabeçalhos compactos.
 * O rótulo visível nomeia o select (aria-labelledby) e a borda/foco ficam no conjunto.
 */
export function InlineSelect({ label, className, children, ...props }: { label: string; className?: string; children: ReactNode } & Omit<SelectHTMLAttributes<HTMLSelectElement>, 'className'>) {
  const id = useId()
  return <label className={`select-inline${className ? ` ${className}` : ''}`}>
    <span id={`${id}-label`} className="select-inline-label">{label}</span>
    <select aria-labelledby={`${id}-label`} {...props}>{children}</select>
  </label>
}
