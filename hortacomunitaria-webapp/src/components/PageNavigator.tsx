import { ChevronLeftIcon, ChevronRightIcon } from 'lucide-react'
import type { Page } from '../types/api'
import { Button } from './ui/button'
import { cn } from './libs/utils'

/** Páginas exibidas por grupo, como na paginação do DataGrid (Metronic). */
const groupSize = 5

// 36 px com mouse; 44 px em telas de toque.
// `shadow-none`/`rounded-md` neutralizam estilos de botão herdados de painéis (ex.: estação de cultivo).
const buttonClasses = 'size-9 min-h-9 rounded-md p-0 text-sm shadow-none pointer-coarse:size-11 pointer-coarse:min-h-11'

/**
 * Troca de página no padrão visual da paginação Metronic (intervalo, setas, números em grupos e reticências),
 * sem seletor de itens por página: o tamanho é definido pela consulta.
 */
export function PageNavigator({ data, change, label, disabled = false, className }: {
  data: Pick<Page<unknown>, 'page' | 'pageSize' | 'totalCount' | 'totalPages'>
  change: (page: number) => void
  label: string
  disabled?: boolean
  className?: string
}) {
  const pageCount = Math.max(1, data.totalPages)
  const from = data.totalCount === 0 ? 0 : (data.page - 1) * data.pageSize + 1
  const to = Math.min(data.page * data.pageSize, data.totalCount)
  const groupStart = Math.floor((data.page - 1) / groupSize) * groupSize + 1
  const groupEnd = Math.min(groupStart + groupSize - 1, pageCount)
  const pages = Array.from({ length: groupEnd - groupStart + 1 }, (_, index) => groupStart + index)
  const go = (page: number) => { if (!disabled && page !== data.page && page >= 1 && page <= pageCount) change(page) }

  return <nav aria-label={label} className={cn('flex flex-wrap items-center justify-between gap-x-3 gap-y-2 py-2', disabled && 'opacity-60', className)}>
    <span className="text-xs text-nowrap text-horta-muted" aria-live="polite">{from} - {to} de {data.totalCount}</span>
    {pageCount > 1 && <div className="flex items-center gap-0.5">
      <Button type="button" variant="ghost" className={cn(buttonClasses, 'rtl:rotate-180')} disabled={disabled || data.page <= 1} onClick={() => go(data.page - 1)}>
        <span className="sr-only">Página anterior</span><ChevronLeftIcon aria-hidden="true" className="size-4" />
      </Button>
      {groupStart > 1 && <Button type="button" variant="ghost" className={buttonClasses} aria-label={`Páginas ${Math.max(1, groupStart - groupSize)} a ${groupStart - 1}`} disabled={disabled} onClick={() => go(groupStart - 1)}>…</Button>}
      {pages.map(page => {
        const current = page === data.page
        return <Button key={page} type="button" variant="ghost" aria-label={`Página ${page}`} aria-current={current ? 'page' : undefined} disabled={disabled}
          className={cn(buttonClasses, 'rounded-full text-horta-muted', current && 'bg-horta-forest font-semibold text-white hover:bg-horta-forest hover:text-white')}
          onClick={() => go(page)}>{page}</Button>
      })}
      {groupEnd < pageCount && <Button type="button" variant="ghost" className={buttonClasses} aria-label={`Páginas ${groupEnd + 1} a ${Math.min(pageCount, groupEnd + groupSize)}`} disabled={disabled} onClick={() => go(groupEnd + 1)}>…</Button>}
      <Button type="button" variant="ghost" className={cn(buttonClasses, 'rtl:rotate-180')} disabled={disabled || data.page >= pageCount} onClick={() => go(data.page + 1)}>
        <span className="sr-only">Próxima página</span><ChevronRightIcon aria-hidden="true" className="size-4" />
      </Button>
    </div>}
  </nav>
}
